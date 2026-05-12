using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
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
}
