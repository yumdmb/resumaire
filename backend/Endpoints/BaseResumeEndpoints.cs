using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Infrastructure.Api;
using Resumaire.Api.Infrastructure.Auth;

namespace Resumaire.Api.Endpoints;

public static class BaseResumeEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapBaseResumeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/resume/base")
            .RequireAuthorization()
            .WithTags("Resume Profile");

        group.MapGet("", GetBaseResumeAsync)
            .WithName("GetBaseResume");

        group.MapPut("", SaveBaseResumeAsync)
            .WithName("SaveBaseResume");

        return group;
    }

    private static async Task<IResult> GetBaseResumeAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.BaseResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);

        return resume is null
            ? Results.NotFound()
            : ApiResponses.Ok(MapBaseResumeResponse(resume));
    }

    private static async Task<IResult> SaveBaseResumeAsync(
        SaveBaseResumeRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = ResumeContentValidator.Validate(request.Content);
        if (validationErrors.Count > 0)
        {
            return validationErrors.ToValidationProblem();
        }

        var userId = httpContext.User.GetUserId();
        var now = DateTimeOffset.UtcNow;
        var contentJson = JsonSerializer.Serialize(request.Content, JsonOptions);
        var resume = await dbContext.BaseResumes
            .SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);

        if (resume is null)
        {
            resume = new BaseResume
            {
                UserId = userId,
                SchemaVersion = ResumeContentSchema.CurrentVersion,
                Revision = 1,
                ContentJson = contentJson,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.BaseResumes.Add(resume);
        }
        else
        {
            resume.SchemaVersion = ResumeContentSchema.CurrentVersion;
            resume.Revision += 1;
            resume.ContentJson = contentJson;
            resume.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponses.Ok(MapBaseResumeResponse(resume));
    }

    private static BaseResumeResponse MapBaseResumeResponse(BaseResume resume)
    {
        var content = JsonSerializer.Deserialize<ResumeContentDto>(resume.ContentJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored base resume content is empty.");

        return new BaseResumeResponse(
            resume.Id,
            resume.SchemaVersion,
            resume.Revision,
            content,
            resume.CreatedAt,
            resume.UpdatedAt);
    }
}

public sealed record SaveBaseResumeRequest(ResumeContentDto? Content);

public sealed record BaseResumeResponse(
    Guid Id,
    int SchemaVersion,
    int Revision,
    ResumeContentDto Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
