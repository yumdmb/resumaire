using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Infrastructure.Api;
using Resumaire.Api.Infrastructure.Auth;
using Resumaire.Api.Tailoring;

namespace Resumaire.Api.Endpoints;

public static class TailoringEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapTailoringEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/jobs/{jobId:guid}/tailoring")
            .RequireAuthorization()
            .WithTags("Resume Tailoring");

        group.MapGet("/analysis", AnalyzeJobAsync)
            .WithName("AnalyzeJobTailoring");

        group.MapPost("/suggestions", GenerateSuggestionsAsync)
            .WithName("GenerateJobTailoringSuggestions");

        group.MapGet("/versions", ListVersionsAsync)
            .WithName("ListJobTailoredResumeVersions");

        group.MapPost("/versions", SaveVersionAsync)
            .WithName("SaveJobTailoredResumeVersion");

        return group;
    }

    private static async Task<IResult> AnalyzeJobAsync(
        Guid jobId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        IJobKeywordExtractor keywordExtractor,
        IResumeKeywordComparer keywordComparer,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var job = await dbContext.Jobs
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == jobId && value.UserId == userId, cancellationToken);

        if (job is null)
        {
            return Results.NotFound();
        }

        var baseResumeQuery = dbContext.BaseResumes
            .AsNoTracking()
            .Where(resume => resume.UserId == userId);

        if (job.SelectedBaseResumeId is not null)
        {
            baseResumeQuery = baseResumeQuery.Where(resume => resume.Id == job.SelectedBaseResumeId.Value);
        }

        var baseResume = await baseResumeQuery.SingleOrDefaultAsync(cancellationToken);
        if (baseResume is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Base resume required",
                detail: "Save a base resume before tailoring a job.");
        }

        var resumeContent = JsonSerializer.Deserialize<ResumeContentDto>(baseResume.ContentJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored base resume content is empty.");

        var extraction = keywordExtractor.Extract(job.Description);
        var comparison = keywordComparer.Compare(extraction.Keywords, resumeContent);

        return ApiResponses.Ok(TailoringAnalysisDtoMapper.ToResponse(
            job.Id,
            baseResume.Id,
            baseResume.Revision,
            extraction.Keywords,
            comparison));
    }

    private static async Task<IResult> GenerateSuggestionsAsync(
        Guid jobId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        IJobKeywordExtractor keywordExtractor,
        IResumeKeywordComparer keywordComparer,
        IAiTailoringSuggestionGenerator suggestionGenerator,
        TailoringSuggestionGuardrails guardrails,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var job = await dbContext.Jobs
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == jobId && value.UserId == userId, cancellationToken);

        if (job is null)
        {
            return Results.NotFound();
        }

        var baseResume = await GetSelectedBaseResumeAsync(dbContext, userId, job.SelectedBaseResumeId, cancellationToken);
        if (baseResume is null)
        {
            return BaseResumeRequired();
        }

        var resumeContent = DeserializeResumeContent(baseResume.ContentJson);
        var extraction = keywordExtractor.Extract(job.Description);
        var comparison = keywordComparer.Compare(extraction.Keywords, resumeContent);

        AiTailoringSuggestionGenerationResult generationResult;
        try
        {
            generationResult = await suggestionGenerator.GenerateAsync(
                new AiTailoringSuggestionGenerationRequest(
                    job.Title,
                    job.Company,
                    job.Description,
                    resumeContent,
                    extraction.Keywords,
                    comparison),
                cancellationToken);
        }
        catch (AiTailoringUnavailableException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AI suggestion generation is not configured",
                detail: exception.Message);
        }
        catch (AiTailoringProviderException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "AI suggestion generation failed",
                detail: exception.Message);
        }

        var guardrailResult = guardrails.Validate(generationResult, resumeContent, comparison);
        if (guardrailResult.Suggestions.Count == 0 && generationResult.Suggestions.Count > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "AI suggestions were rejected by guardrails",
                detail: string.Join(" ", guardrailResult.Rejections));
        }

        var now = DateTimeOffset.UtcNow;
        var suggestions = guardrailResult.Suggestions
            .Select(suggestion => new TailoringSuggestion
            {
                UserId = userId,
                JobId = job.Id,
                SourceBaseResumeId = baseResume.Id,
                ReviewState = TailoringSuggestionReviewState.Pending,
                TargetSection = suggestion.TargetSection,
                OriginalContentJson = SerializeOptionalString(suggestion.OriginalContent),
                SuggestedContentJson = JsonSerializer.Serialize(suggestion.SuggestedContent, JsonOptions),
                Rationale = suggestion.Rationale,
                AiNotes = suggestion.AiNotes,
                SourceEvidenceJson = JsonSerializer.Serialize(suggestion.SourceEvidence, JsonOptions),
                CreatedAt = now
            })
            .ToArray();

        dbContext.TailoringSuggestions.AddRange(suggestions);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new TailoringSuggestionBatchResponse(
            job.Id,
            baseResume.Id,
            baseResume.Revision,
            suggestions.Select(MapSuggestionResponse).ToArray(),
            generationResult.GapNotes
                .Select(note => new TailoringGapNoteResponse(note.Keyword, note.Reason))
                .ToArray(),
            guardrailResult.Rejections);

        return ApiResponses.Ok(response);
    }

    private static async Task<IResult> ListVersionsAsync(
        Guid jobId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (!await dbContext.Jobs.AnyAsync(job => job.Id == jobId && job.UserId == userId, cancellationToken))
        {
            return Results.NotFound();
        }

        var versions = await dbContext.TailoredResumes
            .AsNoTracking()
            .Where(resume => resume.UserId == userId && resume.JobId == jobId)
            .OrderByDescending(resume => resume.VersionNumber)
            .ToListAsync(cancellationToken);

        return ApiResponses.Ok(versions.Select(MapTailoredResumeDetailResponse).ToArray());
    }

    private static async Task<IResult> SaveVersionAsync(
        Guid jobId,
        SaveTailoredResumeRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var job = await dbContext.Jobs
            .SingleOrDefaultAsync(value => value.Id == jobId && value.UserId == userId, cancellationToken);

        if (job is null)
        {
            return Results.NotFound();
        }

        var errors = ValidateSaveVersionRequest(request);
        var baseResume = await GetSelectedBaseResumeAsync(dbContext, userId, job.SelectedBaseResumeId, cancellationToken);
        if (baseResume is null)
        {
            return BaseResumeRequired();
        }

        var acceptedSuggestionIds = request.AcceptedSuggestionIds?.Distinct().ToArray() ?? [];
        var rejectedSuggestionIds = request.RejectedSuggestionIds?.Distinct().ToArray() ?? [];
        var duplicateSuggestionIds = acceptedSuggestionIds.Intersect(rejectedSuggestionIds).ToArray();
        if (duplicateSuggestionIds.Length > 0)
        {
            AddValidationError(
                errors,
                nameof(request.AcceptedSuggestionIds),
                "A suggestion cannot be both accepted and rejected.");
        }

        var suggestionIds = acceptedSuggestionIds.Concat(rejectedSuggestionIds).Distinct().ToArray();
        var suggestions = suggestionIds.Length == 0
            ? []
            : await dbContext.TailoringSuggestions
                .Where(suggestion =>
                    suggestion.UserId == userId &&
                    suggestion.JobId == jobId &&
                    suggestionIds.Contains(suggestion.Id))
                .ToListAsync(cancellationToken);

        if (suggestions.Count != suggestionIds.Length)
        {
            AddValidationError(
                errors,
                nameof(request.AcceptedSuggestionIds),
                "One or more suggestions were not found for this job.");
        }

        if (errors.Count > 0)
        {
            return errors.ToValidationProblem();
        }

        var nextVersionNumber = (await dbContext.TailoredResumes
            .Where(resume => resume.UserId == userId && resume.JobId == jobId)
            .Select(resume => (int?)resume.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        var now = DateTimeOffset.UtcNow;
        var tailoredResume = new TailoredResume
        {
            UserId = userId,
            JobId = jobId,
            SourceBaseResumeId = baseResume.Id,
            SourceBaseResumeRevision = baseResume.Revision,
            SourceBaseResumeContentJson = baseResume.ContentJson,
            SchemaVersion = ResumeContentSchema.CurrentVersion,
            VersionNumber = nextVersionNumber,
            Name = NormalizeOptionalText(request.Name),
            ContentJson = JsonSerializer.Serialize(request.Content, JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.TailoredResumes.Add(tailoredResume);

        foreach (var suggestion in suggestions)
        {
            suggestion.TailoredResumeId = tailoredResume.Id;
            suggestion.ReviewedAt = now;

            if (acceptedSuggestionIds.Contains(suggestion.Id))
            {
                suggestion.ReviewState = TailoringSuggestionReviewState.Accepted;
                suggestion.AcceptedContentJson = suggestion.SuggestedContentJson;
                continue;
            }

            suggestion.ReviewState = TailoringSuggestionReviewState.Rejected;
        }

        job.SelectedBaseResumeId ??= baseResume.Id;
        job.SelectedTailoredResumeId = tailoredResume.Id;
        job.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponses.Created(
            $"/api/jobs/{jobId}/tailoring/versions/{tailoredResume.Id}",
            MapTailoredResumeDetailResponse(tailoredResume));
    }

    private static async Task<BaseResume?> GetSelectedBaseResumeAsync(
        ApplicationDbContext dbContext,
        string userId,
        Guid? selectedBaseResumeId,
        CancellationToken cancellationToken)
    {
        var baseResumeQuery = dbContext.BaseResumes
            .Where(resume => resume.UserId == userId);

        if (selectedBaseResumeId is not null)
        {
            baseResumeQuery = baseResumeQuery.Where(resume => resume.Id == selectedBaseResumeId.Value);
        }

        return await baseResumeQuery.SingleOrDefaultAsync(cancellationToken);
    }

    private static ResumeContentDto DeserializeResumeContent(string contentJson) =>
        JsonSerializer.Deserialize<ResumeContentDto>(contentJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored resume content is empty.");

    private static TailoringSuggestionResponse MapSuggestionResponse(TailoringSuggestion suggestion) =>
        new(
            suggestion.Id,
            suggestion.ReviewState.ToString(),
            suggestion.TargetSection,
            DeserializeOptionalString(suggestion.OriginalContentJson),
            DeserializeRequiredString(suggestion.SuggestedContentJson),
            suggestion.Rationale,
            suggestion.AiNotes,
            DeserializeSourceEvidence(suggestion.SourceEvidenceJson),
            suggestion.CreatedAt,
            suggestion.ReviewedAt);

    private static TailoredResumeDetailResponse MapTailoredResumeDetailResponse(TailoredResume resume) =>
        new(
            resume.Id,
            resume.VersionNumber,
            resume.Name,
            resume.JobId,
            resume.SourceBaseResumeId,
            resume.SourceBaseResumeRevision,
            resume.SchemaVersion,
            DeserializeResumeContent(resume.ContentJson),
            resume.CreatedAt,
            resume.UpdatedAt);

    private static Dictionary<string, string[]> ValidateSaveVersionRequest(SaveTailoredResumeRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (request.Content is null)
        {
            AddValidationError(errors, nameof(request.Content), "Tailored resume content is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim().Length > 200)
        {
            AddValidationError(errors, nameof(request.Name), "Name cannot exceed 200 characters.");
        }

        return errors;
    }

    private static IResult BaseResumeRequired() =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Base resume required",
            detail: "Save a base resume before tailoring a job.");

    private static string? SerializeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Serialize(value.Trim(), JsonOptions);

    private static string? DeserializeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : JsonSerializer.Deserialize<string>(value, JsonOptions);

    private static string DeserializeRequiredString(string value) =>
        JsonSerializer.Deserialize<string>(value, JsonOptions)
            ?? throw new InvalidOperationException("Stored suggestion content is empty.");

    private static IReadOnlyCollection<TailoringSourceEvidenceResponse> DeserializeSourceEvidence(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var evidence = JsonSerializer.Deserialize<IReadOnlyCollection<ResumeKeywordEvidence>>(value, JsonOptions) ?? [];
        return evidence
            .Select(item => new TailoringSourceEvidenceResponse(item.Section, item.Path, item.Text))
            .ToArray();
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddValidationError(
        Dictionary<string, string[]> errors,
        string fieldName,
        string message)
    {
        if (errors.TryGetValue(fieldName, out var messages))
        {
            errors[fieldName] = [.. messages, message];
            return;
        }

        errors[fieldName] = [message];
    }
}
