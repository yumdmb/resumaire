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
            .Produces<string>(contentType: "text/html");

        group.MapGet("/tailored/{tailoredResumeId:guid}/preview", PreviewTailoredResumeAsync)
            .WithName("PreviewTailoredResume")
            .Produces<string>(contentType: "text/html");

        group.MapGet("/base/pdf", ExportBaseResumePdfAsync)
            .WithName("ExportBaseResumePdf")
            .Produces<byte[]>(contentType: "application/pdf");

        group.MapGet("/tailored/{tailoredResumeId:guid}/pdf", ExportTailoredResumePdfAsync)
            .WithName("ExportTailoredResumePdf")
            .Produces<byte[]>(contentType: "application/pdf");

        return group;
    }

    private static async Task<IResult> PreviewBaseResumeAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        IResumeHtmlRenderer htmlRenderer,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.BaseResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        if (resume is null)
            return Results.NotFound();

        var content = DeserializeContent(resume.ContentJson);
        var html = htmlRenderer.RenderToHtml(content);

        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> PreviewTailoredResumeAsync(
        Guid tailoredResumeId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        IResumeHtmlRenderer htmlRenderer,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.TailoredResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == tailoredResumeId && r.UserId == userId, cancellationToken);

        if (resume is null)
            return Results.NotFound();

        var content = DeserializeContent(resume.ContentJson);
        var html = htmlRenderer.RenderToHtml(content);

        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> ExportBaseResumePdfAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        IResumeHtmlRenderer htmlRenderer,
        IResumePdfGenerator pdfGenerator,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.BaseResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        if (resume is null)
            return Results.NotFound();

        var content = DeserializeContent(resume.ContentJson);
        var html = htmlRenderer.RenderToHtml(content);
        var pdfBytes = await pdfGenerator.GenerateAsync(html, cancellationToken);

        var fileName = SanitizeFileName(content.PersonalInfo?.FullName, "resume") + ".pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    }

    private static async Task<IResult> ExportTailoredResumePdfAsync(
        Guid tailoredResumeId,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        IResumeHtmlRenderer htmlRenderer,
        IResumePdfGenerator pdfGenerator,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.TailoredResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == tailoredResumeId && r.UserId == userId, cancellationToken);

        if (resume is null)
            return Results.NotFound();

        var content = DeserializeContent(resume.ContentJson);
        var html = htmlRenderer.RenderToHtml(content);
        var pdfBytes = await pdfGenerator.GenerateAsync(html, cancellationToken);

        var fileName = SanitizeFileName(content.PersonalInfo?.FullName, "resume-tailored") + ".pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
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
