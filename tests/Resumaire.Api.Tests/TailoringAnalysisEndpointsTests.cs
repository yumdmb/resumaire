using System.Net;
using System.Net.Http.Json;
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
using Resumaire.Api.Tests.Infrastructure;
using Resumaire.Api.Tailoring;

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

    [Fact]
    public async Task GenerateSuggestionsAndSaveVersion_ForOwner_PersistsSuggestionReviewStatesAndVersion()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithFakeAiGenerator(
            factory,
            new AiTailoringSuggestionGenerationResult(
                [
                    new AiTailoringSuggestionDraft(
                        "Experience",
                        "Built a React dashboard for API workflow review.",
                        "Built a React dashboard used to review API workflows.",
                        "The job emphasizes React, and this existing bullet already supports that keyword.",
                        [ "Experience[0].Bullets[0]" ],
                        "No new experience was added.",
                        "Experience[0].Bullets[0]",
                        TailoringOperations.Replace),
                    new AiTailoringSuggestionDraft(
                        "Summary",
                        "Builds reliable APIs with PostgreSQL-backed services.",
                        "Builds reliable APIs with PostgreSQL-backed services.",
                        "The job emphasizes APIs, and the summary already supports that keyword.",
                        [ "Summary" ],
                        "No new experience was added.",
                        "Summary",
                        TailoringOperations.Replace)
                ],
                [
                    new AiTailoringGapNote(
                        "Docker",
                        "Docker appears in the job description but is not supported by the base resume evidence.")
                ]));

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var suggestionsResponse = await client.PostAsync($"/api/jobs/{jobId}/tailoring/suggestions", content: null);

        Assert.Equal(HttpStatusCode.OK, suggestionsResponse.StatusCode);
        var suggestionsPayload = await ReadJsonAsync(suggestionsResponse);
        var suggestions = suggestionsPayload.GetProperty("data").GetProperty("suggestions").EnumerateArray().ToArray();
        Assert.Equal(2, suggestions.Length);
        var acceptedSuggestionId = suggestions[0].GetProperty("id").GetGuid();
        var rejectedSuggestionId = suggestions[1].GetProperty("id").GetGuid();
        Assert.All(suggestions, suggestion => Assert.Equal("Pending", suggestion.GetProperty("reviewState").GetString()));
        Assert.Equal("Experience", suggestions[0].GetProperty("targetSection").GetString());
        Assert.Equal("Summary", suggestions[1].GetProperty("targetSection").GetString());

        Assert.Equal("Experience[0].Bullets[0]", suggestions[0].GetProperty("targetPath").GetString());
        Assert.Equal("Replace", suggestions[0].GetProperty("operation").GetString());

        var saveResponse = await client.PostAsJsonAsync($"/api/jobs/{jobId}/tailoring/versions", new
        {
            Name = "Example Co tailored resume",
            AcceptedSuggestionIds = new[] { acceptedSuggestionId },
            RejectedSuggestionIds = new[] { rejectedSuggestionId },
            SuggestionEdits = new Dictionary<Guid, string>
            {
                [acceptedSuggestionId] = "Built a React dashboard used to review API workflows end to end."
            }
        });

        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);
        var savedPayload = await ReadJsonAsync(saveResponse);
        var savedVersion = savedPayload.GetProperty("data");
        var versionId = savedVersion.GetProperty("id").GetGuid();
        Assert.Equal(1, savedVersion.GetProperty("versionNumber").GetInt32());

        // The server applied the user's edit to exactly the targeted bullet and nothing else.
        var savedContent = savedVersion.GetProperty("content");
        Assert.Equal(
            "Built a React dashboard used to review API workflows end to end.",
            savedContent.GetProperty("experience")[0].GetProperty("bullets")[0].GetString());
        Assert.Equal("Backend Engineer", savedContent.GetProperty("experience")[0].GetProperty("role").GetString());
        Assert.Equal("Builds reliable APIs with PostgreSQL-backed services.", savedContent.GetProperty("summary").GetString());
        Assert.Equal(baseResumeId, savedVersion.GetProperty("sourceBaseResumeId").GetGuid());

        var listResponse = await client.GetAsync($"/api/jobs/{jobId}/tailoring/versions");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = await ReadJsonAsync(listResponse);
        var versions = listPayload.GetProperty("data").EnumerateArray().ToArray();
        var listedVersion = Assert.Single(versions);
        Assert.Equal(versionId, listedVersion.GetProperty("id").GetGuid());
        Assert.Equal("Ada Lovelace", listedVersion.GetProperty("content").GetProperty("personalInfo").GetProperty("fullName").GetString());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedAcceptedSuggestion = await dbContext.TailoringSuggestions.SingleAsync(value => value.Id == acceptedSuggestionId);
        var storedRejectedSuggestion = await dbContext.TailoringSuggestions.SingleAsync(value => value.Id == rejectedSuggestionId);
        var storedJob = await dbContext.Jobs.SingleAsync(value => value.Id == jobId);
        Assert.Equal(TailoringSuggestionReviewState.Accepted, storedAcceptedSuggestion.ReviewState);
        Assert.Equal(versionId, storedAcceptedSuggestion.TailoredResumeId);
        Assert.Contains("end to end", storedAcceptedSuggestion.AcceptedContentJson);
        Assert.Equal(TailoringSuggestionReviewState.Rejected, storedRejectedSuggestion.ReviewState);
        Assert.Equal(versionId, storedRejectedSuggestion.TailoredResumeId);
        Assert.Null(storedRejectedSuggestion.AcceptedContentJson);
        Assert.Equal(versionId, storedJob.SelectedTailoredResumeId);
    }

    [Fact]
    public async Task SaveVersion_WithoutSuggestionIds_PersistsManualEditAndListsSavedVersion()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        var manualContent = CreateResumeContent() with
        {
            Summary = "Manual tailored summary for the role."
        };

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var saveResponse = await client.PostAsJsonAsync($"/api/jobs/{jobId}/tailoring/versions", new
        {
            Name = "Manual version",
            Content = manualContent,
            AcceptedSuggestionIds = Array.Empty<Guid>(),
            RejectedSuggestionIds = Array.Empty<Guid>()
        });

        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);
        var savedPayload = await ReadJsonAsync(saveResponse);
        var savedVersion = savedPayload.GetProperty("data");
        var versionId = savedVersion.GetProperty("id").GetGuid();
        Assert.Equal("Manual tailored summary for the role.", savedVersion.GetProperty("content").GetProperty("summary").GetString());

        var listResponse = await client.GetAsync($"/api/jobs/{jobId}/tailoring/versions");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = await ReadJsonAsync(listResponse);
        var listedVersion = Assert.Single(listPayload.GetProperty("data").EnumerateArray());
        Assert.Equal(versionId, listedVersion.GetProperty("id").GetGuid());
        Assert.Equal("Manual version", listedVersion.GetProperty("name").GetString());
        Assert.Equal("Manual tailored summary for the role.", listedVersion.GetProperty("content").GetProperty("summary").GetString());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedJob = await dbContext.Jobs.SingleAsync(value => value.Id == jobId);
        Assert.Equal(versionId, storedJob.SelectedTailoredResumeId);
        Assert.False(await dbContext.TailoringSuggestions.AnyAsync(value => value.JobId == jobId));
    }

    [Fact]
    public async Task GenerateSuggestions_WhenAiAddsUnsupportedKeyword_ReturnsUnprocessableEntity()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithFakeAiGenerator(
            factory,
            new AiTailoringSuggestionGenerationResult(
                [
                    new AiTailoringSuggestionDraft(
                        "Experience",
                        "Built a React dashboard for API workflow review.",
                        "Built a Docker and Kubernetes platform for API workflow review.",
                        "This improperly tries to add missing job keywords.",
                        [ "Experience[0].Bullets[0]" ],
                        "Invalid fabricated tooling.",
                        "Experience[0].Bullets[0]",
                        TailoringOperations.Replace)
                ],
                []));

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.PostAsync($"/api/jobs/{jobId}/tailoring/suggestions", content: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.TailoringSuggestions.AnyAsync(value => value.JobId == jobId));
    }

    [Fact]
    public async Task GenerateSuggestions_WhenProviderFails_ReturnsBadGatewayWithReason()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithThrowingAiGenerator(factory, new AiTailoringProviderException("AI provider timed out."));

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.PostAsync($"/api/jobs/{jobId}/tailoring/suggestions", content: null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("AI provider timed out.", (await ReadJsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GenerateSuggestions_WhenNotConfigured_ReturnsServiceUnavailable()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithThrowingAiGenerator(factory, new AiTailoringUnavailableException("OpenAI:ApiKey is not configured."));

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.PostAsync($"/api/jobs/{jobId}/tailoring/suggestions", content: null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GenerateSuggestions_WhenRequestedRepeatedly_IsRateLimitedPerUser()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithFakeAiGenerator(factory, ReplaceBulletResult()).WithWebHostBuilder(builder =>
            builder.UseSetting("AiTailoring:RequestsPerMinute", "2"));

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);
        var url = $"/api/jobs/{jobId}/tailoring/suggestions";

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(url, content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(url, content: null)).StatusCode);
        var limited = await client.PostAsync(url, content: null);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(
            "Too many AI suggestion requests",
            (await ReadJsonAsync(limited)).GetProperty("title").GetString());

        // Other endpoints are unaffected.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/jobs/{jobId}/tailoring/analysis")).StatusCode);
    }

    [Fact]
    public async Task GenerateSuggestions_Regenerated_SupersedesEarlierPendingSuggestions()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithFakeAiGenerator(factory, ReplaceBulletResult());

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);
        var url = $"/api/jobs/{jobId}/tailoring/suggestions";

        var firstId = await GenerateSingleSuggestionAsync(client, url);
        var secondId = await GenerateSingleSuggestionAsync(client, url);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            TailoringSuggestionReviewState.Superseded,
            (await dbContext.TailoringSuggestions.SingleAsync(value => value.Id == firstId)).ReviewState);
        Assert.Equal(
            TailoringSuggestionReviewState.Pending,
            (await dbContext.TailoringSuggestions.SingleAsync(value => value.Id == secondId)).ReviewState);

        var saveOld = await client.PostAsJsonAsync($"/api/jobs/{jobId}/tailoring/versions", new
        {
            AcceptedSuggestionIds = new[] { firstId }
        });

        Assert.Equal(HttpStatusCode.Conflict, saveOld.StatusCode);
    }

    [Fact]
    public async Task SaveVersion_WhenBaseResumeChangedAfterGeneration_ReturnsConflictAndSavesNothing()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithFakeAiGenerator(factory, ReplaceBulletResult());

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);
        var suggestionId = await GenerateSingleSuggestionAsync(client, $"/api/jobs/{jobId}/tailoring/suggestions");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.BaseResumes.ExecuteUpdateAsync(setters =>
                setters.SetProperty(resume => resume.Revision, resume => resume.Revision + 1));
        }

        var response = await client.PostAsJsonAsync($"/api/jobs/{jobId}/tailoring/versions", new
        {
            AcceptedSuggestionIds = new[] { suggestionId }
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await verifyContext.TailoredResumes.AnyAsync(value => value.JobId == jobId));
    }

    [Fact]
    public async Task SaveVersion_WhenSuggestionAlreadySaved_ReturnsConflict()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        using var appFactory = WithFakeAiGenerator(factory, ReplaceBulletResult());

        var client = appFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);
        var suggestionId = await GenerateSingleSuggestionAsync(client, $"/api/jobs/{jobId}/tailoring/suggestions");
        var saveUrl = $"/api/jobs/{jobId}/tailoring/versions";

        var first = await client.PostAsJsonAsync(saveUrl, new { AcceptedSuggestionIds = new[] { suggestionId } });
        var second = await client.PostAsJsonAsync(saveUrl, new { AcceptedSuggestionIds = new[] { suggestionId } });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task SaveVersion_WithInvalidManualContent_ReturnsValidationProblem()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var baseResumeId = await SeedBaseResumeAsync(factory, OwnerUserId);
        var jobId = await SeedJobAsync(factory, OwnerUserId, baseResumeId);
        var invalid = CreateResumeContent() with { PersonalInfo = new ResumePersonalInfoDto(" ", null, null, null, null, null) };

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.PostAsJsonAsync($"/api/jobs/{jobId}/tailoring/versions", new
        {
            Content = invalid,
            AcceptedSuggestionIds = Array.Empty<Guid>(),
            RejectedSuggestionIds = Array.Empty<Guid>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static AiTailoringSuggestionGenerationResult ReplaceBulletResult() =>
        new(
            [
                new AiTailoringSuggestionDraft(
                    "Experience",
                    null,
                    "Built a React dashboard used to review API workflows.",
                    "Grounded in the existing bullet.",
                    [ "Experience[0].Bullets[0]" ],
                    null,
                    "Experience[0].Bullets[0]",
                    TailoringOperations.Replace)
            ],
            []);

    private static async Task<Guid> GenerateSingleSuggestionAsync(HttpClient client, string url)
    {
        var response = await client.PostAsync(url, content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = (await ReadJsonAsync(response)).GetProperty("data").GetProperty("suggestions");
        return suggestions[0].GetProperty("id").GetGuid();
    }

    private static WebApplicationFactory<Program> WithThrowingAiGenerator(
        ResumaireApiFactory factory,
        Exception exception) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiTailoringSuggestionGenerator>();
                services.AddSingleton<IAiTailoringSuggestionGenerator>(new ThrowingAiTailoringSuggestionGenerator(exception));
            });
        });

    private async Task<ResumaireApiFactory> CreateMigratedFactoryAsync()
    {
        var factory = new ResumaireApiFactory().WithSqlite();
        await factory.MigrateDatabaseAsync();
        return factory;
    }

    private static WebApplicationFactory<Program> WithFakeAiGenerator(
        ResumaireApiFactory factory,
        AiTailoringSuggestionGenerationResult result) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiTailoringSuggestionGenerator>();
                services.AddSingleton<IAiTailoringSuggestionGenerator>(
                    new FakeAiTailoringSuggestionGenerator(result));
            });
        });

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

    private sealed class FakeAiTailoringSuggestionGenerator(AiTailoringSuggestionGenerationResult result)
        : IAiTailoringSuggestionGenerator
    {
        public Task<AiTailoringSuggestionGenerationResult> GenerateAsync(
            AiTailoringSuggestionGenerationRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class ThrowingAiTailoringSuggestionGenerator(Exception exception)
        : IAiTailoringSuggestionGenerator
    {
        public Task<AiTailoringSuggestionGenerationResult> GenerateAsync(
            AiTailoringSuggestionGenerationRequest request,
            CancellationToken cancellationToken) =>
            throw exception;
    }

    private const string OwnerUserId = "owner-user";
    private const string OtherUserId = "other-user";
}
