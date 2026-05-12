using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Infrastructure.Api;
using Resumaire.Api.Infrastructure.Auth;

namespace Resumaire.Api.Endpoints;

public static class JobsEndpoints
{
    public static RouteGroupBuilder MapJobsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/jobs")
            .RequireAuthorization()
            .WithTags("Jobs");

        group.MapGet("", ListJobsAsync)
            .WithName("ListJobs");

        group.MapPost("", CreateJobAsync)
            .WithName("CreateJob");

        group.MapGet("/{jobId:guid}", GetJobAsync)
            .WithName("GetJob");

        group.MapPut("/{jobId:guid}", UpdateJobAsync)
            .WithName("UpdateJob");

        group.MapDelete("/{jobId:guid}", DeleteJobAsync)
            .WithName("DeleteJob");

        return group;
    }

    private static async Task<IResult> ListJobsAsync(
        string? status,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();

        JobStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseJobStatus(status, out var parsedStatus))
            {
                return ValidationProblem(nameof(status), UnsupportedStatusMessage);
            }

            statusFilter = parsedStatus;
        }

        var query = dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.UserId == userId);

        if (statusFilter is not null)
        {
            query = query.Where(job => job.Status == statusFilter.Value);
        }

        if (dbContext.Database.IsSqlite())
        {
            var sqliteJobs = await query
                .Select(job => new JobSummaryResponse(
                    job.Id,
                    job.Company,
                    job.Title,
                    job.Link,
                    job.Status.ToString(),
                    job.DateApplied,
                    job.Notes,
                    job.SelectedBaseResumeId,
                    job.SelectedTailoredResumeId,
                    job.CreatedAt,
                    job.UpdatedAt))
                .ToListAsync(cancellationToken);

            return ApiResponses.Ok(sqliteJobs.OrderByDescending(job => job.UpdatedAt).ToList());
        }

        var jobs = await query
            .OrderByDescending(job => job.UpdatedAt)
            .Select(job => new JobSummaryResponse(
                job.Id,
                job.Company,
                job.Title,
                job.Link,
                job.Status.ToString(),
                job.DateApplied,
                job.Notes,
                job.SelectedBaseResumeId,
                job.SelectedTailoredResumeId,
                job.CreatedAt,
                job.UpdatedAt))
            .ToListAsync(cancellationToken);

        return ApiResponses.Ok(jobs);
    }

    private static async Task<IResult> CreateJobAsync(
        JobRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var validationErrors = ValidateJobRequest(request, requireStatus: false);

        if (!TryParseJobStatus(request.Status, out var status))
        {
            AddValidationError(validationErrors, nameof(request.Status), UnsupportedStatusMessage);
        }

        if (request.SelectedTailoredResumeId is not null)
        {
            AddValidationError(
                validationErrors,
                nameof(request.SelectedTailoredResumeId),
                "A tailored resume can only be selected after the job exists.");
        }

        if (request.SelectedBaseResumeId is not null &&
            !await UserOwnsBaseResumeAsync(dbContext, userId, request.SelectedBaseResumeId.Value, cancellationToken))
        {
            AddValidationError(
                validationErrors,
                nameof(request.SelectedBaseResumeId),
                "The selected base resume was not found.");
        }

        if (validationErrors.Count > 0)
        {
            return validationErrors.ToValidationProblem();
        }

        var now = DateTimeOffset.UtcNow;
        var job = new Job
        {
            UserId = userId,
            Company = request.Company!.Trim(),
            Title = request.Title!.Trim(),
            Link = NormalizeOptionalText(request.Link),
            Description = request.Description!.Trim(),
            Status = status,
            DateApplied = request.DateApplied,
            Notes = NormalizeOptionalText(request.Notes),
            SelectedBaseResumeId = request.SelectedBaseResumeId,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponses.Created($"/api/jobs/{job.Id}", MapJobResponse(job));
    }

    private static async Task<IResult> GetJobAsync(
        Guid jobId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();

        var detail = await dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.Id == jobId && job.UserId == userId)
            .Select(job => new JobDetailResponse(
                job.Id,
                job.Company,
                job.Title,
                job.Link,
                job.Description,
                job.Status.ToString(),
                job.DateApplied,
                job.Notes,
                job.SelectedBaseResumeId,
                job.SelectedTailoredResumeId,
                job.CreatedAt,
                job.UpdatedAt,
                job.TailoredResumes
                    .OrderByDescending(resume => resume.VersionNumber)
                    .Select(resume => new TailoredResumeVersionResponse(
                        resume.Id,
                        resume.VersionNumber,
                        resume.Name,
                        resume.SourceBaseResumeId,
                        resume.SourceBaseResumeRevision,
                        resume.SchemaVersion,
                        resume.CreatedAt,
                        resume.UpdatedAt))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return detail is null
            ? Results.NotFound()
            : ApiResponses.Ok(detail);
    }

    private static async Task<IResult> UpdateJobAsync(
        Guid jobId,
        JobRequest request,
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

        var validationErrors = ValidateJobRequest(request, requireStatus: true);

        if (!TryParseJobStatus(request.Status, out var status))
        {
            AddValidationError(validationErrors, nameof(request.Status), UnsupportedStatusMessage);
        }

        if (request.SelectedBaseResumeId is not null &&
            !await UserOwnsBaseResumeAsync(dbContext, userId, request.SelectedBaseResumeId.Value, cancellationToken))
        {
            AddValidationError(
                validationErrors,
                nameof(request.SelectedBaseResumeId),
                "The selected base resume was not found.");
        }

        if (request.SelectedTailoredResumeId is not null &&
            !await UserOwnsTailoredResumeAsync(dbContext, userId, jobId, request.SelectedTailoredResumeId.Value, cancellationToken))
        {
            AddValidationError(
                validationErrors,
                nameof(request.SelectedTailoredResumeId),
                "The selected tailored resume was not found for this job.");
        }

        if (validationErrors.Count > 0)
        {
            return validationErrors.ToValidationProblem();
        }

        job.Company = request.Company!.Trim();
        job.Title = request.Title!.Trim();
        job.Link = NormalizeOptionalText(request.Link);
        job.Description = request.Description!.Trim();
        job.Status = status;
        job.DateApplied = request.DateApplied;
        job.Notes = NormalizeOptionalText(request.Notes);
        job.SelectedBaseResumeId = request.SelectedBaseResumeId;
        job.SelectedTailoredResumeId = request.SelectedTailoredResumeId;
        job.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponses.Ok(MapJobResponse(job));
    }

    private static async Task<IResult> DeleteJobAsync(
        Guid jobId,
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

        dbContext.Jobs.Remove(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static JobResponse MapJobResponse(Job job) =>
        new(
            job.Id,
            job.Company,
            job.Title,
            job.Link,
            job.Description,
            job.Status.ToString(),
            job.DateApplied,
            job.Notes,
            job.SelectedBaseResumeId,
            job.SelectedTailoredResumeId,
            job.CreatedAt,
            job.UpdatedAt);

    private static Dictionary<string, string[]> ValidateJobRequest(JobRequest request, bool requireStatus)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        ValidateRequiredText(errors, nameof(request.Company), request.Company, maxLength: 200);
        ValidateRequiredText(errors, nameof(request.Title), request.Title, maxLength: 200);
        ValidateRequiredText(errors, nameof(request.Description), request.Description);

        if (requireStatus && string.IsNullOrWhiteSpace(request.Status))
        {
            AddValidationError(errors, nameof(request.Status), "Status is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.Link))
        {
            if (request.Link.Length > 2048)
            {
                AddValidationError(errors, nameof(request.Link), "Link cannot exceed 2048 characters.");
            }

            if (!Uri.TryCreate(request.Link, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                AddValidationError(errors, nameof(request.Link), "Link must be an absolute HTTP or HTTPS URL.");
            }
        }

        return errors;
    }

    private static void ValidateRequiredText(
        Dictionary<string, string[]> errors,
        string fieldName,
        string? value,
        int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddValidationError(errors, fieldName, $"{fieldName} is required.");
            return;
        }

        if (maxLength is not null && value.Trim().Length > maxLength.Value)
        {
            AddValidationError(errors, fieldName, $"{fieldName} cannot exceed {maxLength.Value} characters.");
        }
    }

    private static bool TryParseJobStatus(string? value, out JobStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = JobStatus.Saved;
            return true;
        }

        foreach (var supportedStatus in Enum.GetNames<JobStatus>())
        {
            if (string.Equals(supportedStatus, value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                status = Enum.Parse<JobStatus>(supportedStatus);
                return true;
            }
        }

        status = JobStatus.Saved;
        return false;
    }

    private static async Task<bool> UserOwnsBaseResumeAsync(
        ApplicationDbContext dbContext,
        string userId,
        Guid baseResumeId,
        CancellationToken cancellationToken) =>
        await dbContext.BaseResumes.AnyAsync(
            resume => resume.Id == baseResumeId && resume.UserId == userId,
            cancellationToken);

    private static async Task<bool> UserOwnsTailoredResumeAsync(
        ApplicationDbContext dbContext,
        string userId,
        Guid jobId,
        Guid tailoredResumeId,
        CancellationToken cancellationToken) =>
        await dbContext.TailoredResumes.AnyAsync(
            resume => resume.Id == tailoredResumeId &&
                resume.UserId == userId &&
                resume.JobId == jobId,
            cancellationToken);

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IResult ValidationProblem(string fieldName, string message)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddValidationError(errors, fieldName, message);
        return errors.ToValidationProblem();
    }

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

    private const string UnsupportedStatusMessage =
        "Status must be one of: Saved, Applied, Interview, Rejected, Offer.";
}

public sealed record JobRequest(
    string? Company,
    string? Title,
    string? Link,
    string? Description,
    string? Status,
    DateOnly? DateApplied,
    string? Notes,
    Guid? SelectedBaseResumeId,
    Guid? SelectedTailoredResumeId);

public sealed record JobSummaryResponse(
    Guid Id,
    string Company,
    string Title,
    string? Link,
    string Status,
    DateOnly? DateApplied,
    string? Notes,
    Guid? SelectedBaseResumeId,
    Guid? SelectedTailoredResumeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record JobResponse(
    Guid Id,
    string Company,
    string Title,
    string? Link,
    string Description,
    string Status,
    DateOnly? DateApplied,
    string? Notes,
    Guid? SelectedBaseResumeId,
    Guid? SelectedTailoredResumeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record JobDetailResponse(
    Guid Id,
    string Company,
    string Title,
    string? Link,
    string Description,
    string Status,
    DateOnly? DateApplied,
    string? Notes,
    Guid? SelectedBaseResumeId,
    Guid? SelectedTailoredResumeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<TailoredResumeVersionResponse> TailoredResumeVersions);

public sealed record TailoredResumeVersionResponse(
    Guid Id,
    int VersionNumber,
    string? Name,
    Guid SourceBaseResumeId,
    int SourceBaseResumeRevision,
    int SchemaVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
