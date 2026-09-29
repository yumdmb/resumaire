using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
using Resumaire.Api.Export;
using Resumaire.Api.Infrastructure.Auth;

namespace Resumaire.Api.Endpoints;

public static class ExportEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/export")
            .RequireAuthorization()
            .WithTags("Resume Export");

        group.MapGet("/base/preview", PreviewBaseResumeAsync)
            .WithName("PreviewBaseResume")
            .Produces<byte[]>(contentType: "application/pdf");

        group.MapGet("/tailored/{tailoredResumeId:guid}/preview", PreviewTailoredResumeAsync)
            .WithName("PreviewTailoredResume")
            .Produces<byte[]>(contentType: "application/pdf");

        group.MapGet("/base/pdf", ExportBaseResumePdfAsync)
            .WithName("ExportBaseResumePdf")
            .Produces<byte[]>(contentType: "application/pdf");

        group.MapGet("/tailored/{tailoredResumeId:guid}/pdf", ExportTailoredResumePdfAsync)
            .WithName("ExportTailoredResumePdf")
            .Produces<byte[]>(contentType: "application/pdf");

        return group;
    }

    private static Task<IResult> PreviewBaseResumeAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        ITypstPdfRenderer renderer,
        CancellationToken cancellationToken) =>
        RenderBaseAsync(dbContext, httpContext, renderer, "resume", inline: true, cancellationToken);

    private static Task<IResult> PreviewTailoredResumeAsync(
        Guid tailoredResumeId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        ITypstPdfRenderer renderer,
        CancellationToken cancellationToken) =>
        RenderTailoredAsync(tailoredResumeId, dbContext, httpContext, renderer, "resume-tailored", inline: true, cancellationToken);

    private static Task<IResult> ExportBaseResumePdfAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        ITypstPdfRenderer renderer,
        CancellationToken cancellationToken) =>
        RenderBaseAsync(dbContext, httpContext, renderer, "resume", inline: false, cancellationToken);

    private static Task<IResult> ExportTailoredResumePdfAsync(
        Guid tailoredResumeId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        ITypstPdfRenderer renderer,
        CancellationToken cancellationToken) =>
        RenderTailoredAsync(tailoredResumeId, dbContext, httpContext, renderer, "resume-tailored", inline: false, cancellationToken);

    private static async Task<IResult> RenderBaseAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        ITypstPdfRenderer renderer,
        string fallbackName,
        bool inline,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.BaseResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        return resume is null
            ? Results.NotFound()
            : await RenderAsync(resume.ContentJson, renderer, fallbackName, inline, cancellationToken);
    }

    private static async Task<IResult> RenderTailoredAsync(
        Guid tailoredResumeId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        ITypstPdfRenderer renderer,
        string fallbackName,
        bool inline,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.TailoredResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == tailoredResumeId && r.UserId == userId, cancellationToken);

        return resume is null
            ? Results.NotFound()
            : await RenderAsync(resume.ContentJson, renderer, fallbackName, inline, cancellationToken);
    }

    private static async Task<IResult> RenderAsync(
        string contentJson,
        ITypstPdfRenderer renderer,
        string fallbackName,
        bool inline,
        CancellationToken cancellationToken)
    {
        var content = DeserializeContent(contentJson);

        byte[] pdfBytes;
        try
        {
            pdfBytes = await renderer.RenderAsync(content, cancellationToken);
        }
        catch (ResumeRenderException ex)
        {
            return Results.Problem(
                title: "Resume PDF rendering failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }

        var fileName = SanitizeFileName(content.PersonalInfo?.FullName, fallbackName) + ".pdf";
        return Results.File(pdfBytes, "application/pdf", inline ? null : fileName);
    }

    private static ResumeContentDto DeserializeContent(string contentJson) =>
        JsonSerializer.Deserialize<ResumeContentDto>(contentJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored resume content is empty.");

    private static string SanitizeFileName(string? name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name))
            return fallback;

        var sanitized = string.Concat(name.Trim().Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized.Replace(' ', '-').ToLowerInvariant();
    }
}
