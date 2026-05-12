using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class TailoringAnalysisEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AnalyzeJobTailoring_ForOwner_ExtractsKeywordsAndComparesBaseResume()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/jobs/{jobId}/tailoring/analysis");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var data = payload.GetProperty("data");
        Assert.Equal(jobId, data.GetProperty("jobId").GetGuid());
        Assert.Equal(baseResumeId, data.GetProperty("baseResumeId").GetGuid());

        var extractedKeywords = data.GetProperty("extractedKeywords").EnumerateArray().ToArray();
        Assert.Contains(extractedKeywords, keyword => keyword.GetProperty("text").GetString() == "ASP.NET Core");
        Assert.Contains(extractedKeywords, keyword => keyword.GetProperty("text").GetString() == "React");
        Assert.Contains(extractedKeywords, keyword => keyword.GetProperty("text").GetString() == "Docker");
        Assert.Contains(extractedKeywords, keyword => keyword.GetProperty("text").GetString() == "Kubernetes");
        Assert.Contains(extractedKeywords, keyword =>
            keyword.GetProperty("text").GetString() == "Senior" &&
            keyword.GetProperty("category").GetString() == "Seniority");

        var comparison = data.GetProperty("comparison");
        var supportedKeywords = comparison.GetProperty("supportedKeywords").EnumerateArray().ToArray();
        Assert.Contains(supportedKeywords, keyword => keyword.GetProperty("keyword").GetProperty("text").GetString() == "ASP.NET Core");
        Assert.Contains(supportedKeywords, keyword => keyword.GetProperty("keyword").GetProperty("text").GetString() == "React");

        var reactSupport = supportedKeywords.Single(keyword => keyword.GetProperty("keyword").GetProperty("text").GetString() == "React");
        var reactEvidence = reactSupport.GetProperty("evidence").EnumerateArray().ToArray();
        Assert.Contains(reactEvidence, evidence => evidence.GetProperty("path").GetString() == "Experience[0].Bullets[0]");

        var missingKeywords = comparison.GetProperty("missingKeywords").EnumerateArray().ToArray();
        Assert.Contains(missingKeywords, keyword => keyword.GetProperty("keyword").GetProperty("text").GetString() == "Docker");
        Assert.Contains(missingKeywords, keyword => keyword.GetProperty("keyword").GetProperty("text").GetString() == "Kubernetes");

        var reorderOpportunities = comparison.GetProperty("reorderOpportunities").EnumerateArray().ToArray();
        var reactOpportunity = Assert.Single(
            reorderOpportunities,
            opportunity => opportunity.GetProperty("keyword").GetProperty("text").GetString() == "React");
        var suggestedSections = reactOpportunity.GetProperty("suggestedSections").EnumerateArray().Select(section => section.GetString()).ToArray();
        Assert.Equal(new[] { "Summary", "Skills" }, suggestedSections);
    }

    [Fact]
    public async Task AnalyzeJobTailoring_ForDifferentUser_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        await SeedUsersAsync(factory, OtherUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await client.GetAsync($"/api/jobs/{jobId}/tailoring/analysis");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeJobTailoring_WithoutBaseResume_ReturnsConflict()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var jobId = await SeedJobAsync(factory, OwnerUserId, selectedBaseResumeId: null);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync($"/api/jobs/{jobId}/tailoring/analysis");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<ResumaireApiFactory> CreateMigratedFactoryAsync()
    {
        var factory = new ResumaireApiFactory().WithSqlite();
        await factory.MigrateDatabaseAsync();
        return factory;
    }

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
            {
                continue;
            }

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

    private static async Task<Guid> SeedBaseResumeAsync(ResumaireApiFactory factory, string userId)
    {
        await SeedUsersAsync(factory, userId);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var resume = new BaseResume
        {
            UserId = userId,
            SchemaVersion = ResumeContentSchema.CurrentVersion,
            Revision = 3,
            ContentJson = JsonSerializer.Serialize(CreateResumeContent(), JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.BaseResumes.Add(resume);
        await dbContext.SaveChangesAsync();

        return resume.Id;
    }

    private static async Task<Guid> SeedJobAsync(
        ResumaireApiFactory factory,
        string userId,
        Guid? selectedBaseResumeId)
    {
        await SeedUsersAsync(factory, userId);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var job = new Job
        {
            UserId = userId,
            Company = "Example Co",
            Title = "Senior Full Stack Engineer",
            Description = "We need a Senior engineer to build REST APIs with ASP.NET Core, React, Docker, and Kubernetes while mentoring teammates.",
            Status = JobStatus.Saved,
            SelectedBaseResumeId = selectedBaseResumeId,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync();

        return job.Id;
    }

    private static ResumeContentDto CreateResumeContent() =>
        new(
            new ResumePersonalInfoDto(
                "Ada Lovelace",
                "ada@example.test",
                "",
                "London",
                "Backend Engineer",
                "https://example.test"),
            "Builds reliable APIs with PostgreSQL-backed services.",
            [ "ASP.NET Core", "PostgreSQL" ],
            [
                new ResumeExperienceDto(
                    "exp-1",
                    "Backend Engineer",
                    "Example Co",
                    "Remote",
                    "2024-01",
                    "",
                    true,
                    [ "Built a React dashboard for API workflow review." ])
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

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload;
    }

    private const string OwnerUserId = "owner-user";
    private const string OtherUserId = "other-user";
}
