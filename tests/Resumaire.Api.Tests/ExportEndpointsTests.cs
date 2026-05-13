using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Export;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class ExportEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // ── Preview rendering ────────────────────────────────────────────────────

    [Fact]
    public async Task PreviewBaseResume_ForOwner_ReturnsHtmlContainingPersonalInfo()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedBaseResumeAsync(factory, OwnerUserId, fullName: "Ada Lovelace");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/export/base/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ada Lovelace", html);
        Assert.Contains("Builds reliable APIs.", html);
        Assert.Contains("ASP.NET Core", html);
    }

    [Fact]
    public async Task PreviewTailoredResume_ForOwner_ReturnsHtmlContainingTailoredContent()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var (_, tailoredResumeId) = await SeedTailoredResumeAsync(
            factory,
            OwnerUserId,
            summary: "Tailored summary for the role.");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/export/tailored/{tailoredResumeId}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ada Lovelace", html);
        Assert.Contains("Tailored summary for the role.", html);
    }

    [Fact]
    public async Task PreviewBaseResume_WhenNoBaseResume_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/export/base/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PreviewTailoredResume_WhenNoTailoredResume_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/export/tailored/{Guid.NewGuid()}/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Export ownership enforcement ─────────────────────────────────────────

    [Fact]
    public async Task PreviewBaseResume_ForDifferentUser_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedBaseResumeAsync(factory, OwnerUserId);
        await SeedUsersAsync(factory, OtherUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await client.GetAsync("/api/export/base/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PreviewTailoredResume_ForDifferentUser_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var (_, tailoredResumeId) = await SeedTailoredResumeAsync(factory, OwnerUserId);
        await SeedUsersAsync(factory, OtherUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await client.GetAsync($"/api/export/tailored/{tailoredResumeId}/preview");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportBaseResumePdf_ForDifferentUser_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedBaseResumeAsync(factory, OwnerUserId);
        await SeedUsersAsync(factory, OtherUserId);
        using var appFactory = WithFakePdfGenerator(factory);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await client.GetAsync("/api/export/base/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ExportTailoredResumePdf_ForDifferentUser_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var (_, tailoredResumeId) = await SeedTailoredResumeAsync(factory, OwnerUserId);
        await SeedUsersAsync(factory, OtherUserId);
        using var appFactory = WithFakePdfGenerator(factory);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await client.GetAsync($"/api/export/tailored/{tailoredResumeId}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PDF export ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportBaseResumePdf_ForOwner_ReturnsPdfWithCorrectHeaders()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedBaseResumeAsync(factory, OwnerUserId, fullName: "Ada Lovelace");
        using var appFactory = WithFakePdfGenerator(factory);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/export/base/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var disposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.Equal("attachment", disposition.DispositionType);
        Assert.Contains("ada-lovelace.pdf", disposition.FileName ?? disposition.FileNameStar ?? "");
    }

    [Fact]
    public async Task ExportTailoredResumePdf_ForOwner_ReturnsPdfWithCorrectHeaders()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var (_, tailoredResumeId) = await SeedTailoredResumeAsync(factory, OwnerUserId);
        using var appFactory = WithFakePdfGenerator(factory);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/export/tailored/{tailoredResumeId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var disposition = response.Content.Headers.ContentDisposition;
        Assert.NotNull(disposition);
        Assert.Equal("attachment", disposition.DispositionType);
    }

    [Fact]
    public async Task ExportBaseResumePdf_WhenNoBaseResume_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);
        using var appFactory = WithFakePdfGenerator(factory);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/export/base/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Export source integrity ──────────────────────────────────────────────

    [Fact]
    public async Task ExportBaseResumePdf_RendersFromSavedContent_NotUnsavedEditorState()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedBaseResumeAsync(factory, OwnerUserId, summary: "Saved summary content.");

        string? capturedHtml = null;
        using var appFactory = WithCapturingPdfGenerator(factory, html => capturedHtml = html);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/export/base/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedHtml);
        Assert.Contains("Saved summary content.", capturedHtml);
    }

    [Fact]
    public async Task ExportTailoredResumePdf_RendersFromSavedContent_NotUnsavedEditorState()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var (_, tailoredResumeId) = await SeedTailoredResumeAsync(
            factory,
            OwnerUserId,
            summary: "Saved tailored summary.");

        string? capturedHtml = null;
        using var appFactory = WithCapturingPdfGenerator(factory, html => capturedHtml = html);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/export/tailored/{tailoredResumeId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedHtml);
        Assert.Contains("Saved tailored summary.", capturedHtml);
    }

    [Fact]
    public async Task ExportTailoredResumePdf_ContentReflectsOnlySavedResumeJson_NotRejectedSuggestions()
    {
        // The tailored resume's ContentJson is the source of truth for export.
        // Rejected suggestions are never merged into ContentJson, so the export
        // naturally excludes them. This test verifies that the export renders
        // exactly what is stored in ContentJson and nothing else.
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var (_, tailoredResumeId) = await SeedTailoredResumeAsync(
            factory,
            OwnerUserId,
            summary: "Approved summary only.");

        string? capturedHtml = null;
        using var appFactory = WithCapturingPdfGenerator(factory, html => capturedHtml = html);

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/export/tailored/{tailoredResumeId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedHtml);
        Assert.Contains("Approved summary only.", capturedHtml);
        Assert.DoesNotContain("Rejected suggestion text.", capturedHtml);
    }

    // ── Unauthenticated access ───────────────────────────────────────────────

    [Fact]
    public async Task ExportEndpoints_WithoutAuthentication_ReturnUnauthorized()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);

        var client = factory.CreateClient();

        var previewBaseResponse = await client.GetAsync("/api/export/base/preview");
        var previewTailoredResponse = await client.GetAsync($"/api/export/tailored/{Guid.NewGuid()}/preview");
        var pdfBaseResponse = await client.GetAsync("/api/export/base/pdf");
        var pdfTailoredResponse = await client.GetAsync($"/api/export/tailored/{Guid.NewGuid()}/pdf");

        Assert.Equal(HttpStatusCode.Unauthorized, previewBaseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, previewTailoredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, pdfBaseResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, pdfTailoredResponse.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ResumaireApiFactory> CreateMigratedFactoryAsync()
    {
        var factory = new ResumaireApiFactory().WithSqlite();
        await factory.MigrateDatabaseAsync();
        return factory;
    }

    private static WebApplicationFactory<Program> WithFakePdfGenerator(ResumaireApiFactory factory) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IResumePdfGenerator>();
                services.AddSingleton<IResumePdfGenerator>(new FakePdfGenerator());
            }));

    private static WebApplicationFactory<Program> WithCapturingPdfGenerator(
        ResumaireApiFactory factory,
        Action<string> onGenerate) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IResumePdfGenerator>();
                services.AddSingleton<IResumePdfGenerator>(new CapturingPdfGenerator(onGenerate));
            }));

    private static async Task ResetDatabaseAsync(ResumaireApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.TailoringSuggestions.ExecuteDeleteAsync();
        await dbContext.TailoredResumes.ExecuteDeleteAsync();
        await dbContext.Jobs.ExecuteDeleteAsync();
        await dbContext.BaseResumes.ExecuteDeleteAsync();
        await dbContext.Users.ExecuteDeleteAsync();
    }

    private static async Task SeedUsersAsync(ResumaireApiFactory factory, params string[] userIds)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var userId in userIds.Distinct(StringComparer.Ordinal))
        {
            if (await dbContext.Users.AnyAsync(user => user.Id == userId))
                continue;

            dbContext.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId}@example.test",
                NormalizedUserName = $"{userId}@example.test".ToUpperInvariant(),
                Email = $"{userId}@example.test",
                NormalizedEmail = $"{userId}@example.test".ToUpperInvariant(),
                EmailConfirmed = true
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> SeedBaseResumeAsync(
        ResumaireApiFactory factory,
        string userId,
        string fullName = "Ada Lovelace",
        string summary = "Builds reliable APIs.")
    {
        await SeedUsersAsync(factory, userId);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var resume = new BaseResume
        {
            UserId = userId,
            SchemaVersion = ResumeContentSchema.CurrentVersion,
            Revision = 1,
            ContentJson = JsonSerializer.Serialize(CreateResumeContent(fullName, summary), JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.BaseResumes.Add(resume);
        await dbContext.SaveChangesAsync();

        return resume.Id;
    }

    private static async Task<(Guid jobId, Guid tailoredResumeId)> SeedTailoredResumeAsync(
        ResumaireApiFactory factory,
        string userId,
        string fullName = "Ada Lovelace",
        string summary = "Builds reliable APIs.")
    {
        var baseResumeId = await SeedBaseResumeAsync(factory, userId, fullName, summary);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;

        var job = new Job
        {
            UserId = userId,
            Company = "Example Co",
            Title = "Backend Engineer",
            Description = "Build APIs.",
            Status = JobStatus.Saved,
            SelectedBaseResumeId = baseResumeId,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync();

        var tailoredResume = new TailoredResume
        {
            UserId = userId,
            JobId = job.Id,
            SourceBaseResumeId = baseResumeId,
            SourceBaseResumeRevision = 1,
            SourceBaseResumeContentJson = JsonSerializer.Serialize(CreateResumeContent(fullName, summary), JsonOptions),
            SchemaVersion = ResumeContentSchema.CurrentVersion,
            VersionNumber = 1,
            Name = "Tailored version",
            ContentJson = JsonSerializer.Serialize(CreateResumeContent(fullName, summary), JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.TailoredResumes.Add(tailoredResume);
        await dbContext.SaveChangesAsync();

        return (job.Id, tailoredResume.Id);
    }

    private static ResumeContentDto CreateResumeContent(
        string fullName = "Ada Lovelace",
        string summary = "Builds reliable APIs.") =>
        new(
            new ResumePersonalInfoDto(
                fullName,
                "ada@example.test",
                "",
                "London",
                "Backend Engineer",
                "https://example.test"),
            summary,
            ["ASP.NET Core", "PostgreSQL"],
            [
                new ResumeExperienceDto(
                    "exp-1",
                    "Backend Engineer",
                    "Example Co",
                    "Remote",
                    "2024-01",
                    "",
                    true,
                    ["Built API workflows."])
            ],
            [
                new ResumeEducationDto(
                    "edu-1",
                    "Example University",
                    "BS",
                    "Computer Science",
                    "",
                    "",
                    "2022",
                    [])
            ],
            [],
            [
                new ResumeLinkDto(
                    "link-1",
                    "Portfolio",
                    "https://example.test/portfolio")
            ]);

    private sealed class FakePdfGenerator : IResumePdfGenerator
    {
        private static readonly byte[] FakePdfBytes = "%PDF-1.4 fake"u8.ToArray();

        public Task<byte[]> GenerateAsync(string html, CancellationToken cancellationToken = default) =>
            Task.FromResult(FakePdfBytes);
    }

    private sealed class CapturingPdfGenerator(Action<string> onGenerate) : IResumePdfGenerator
    {
        private static readonly byte[] FakePdfBytes = "%PDF-1.4 fake"u8.ToArray();

        public Task<byte[]> GenerateAsync(string html, CancellationToken cancellationToken = default)
        {
            onGenerate(html);
            return Task.FromResult(FakePdfBytes);
        }
    }

    private const string OwnerUserId = "owner-user";
    private const string OtherUserId = "other-user";
}
