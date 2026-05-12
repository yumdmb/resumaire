using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Resumaire.Api.Data;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class BaseResumeEndpointsTests
{
    [Fact]
    public async Task SaveAndGetBaseResume_ForOwner_PersistsStructuredContent()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var saveResponse = await client.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Ada Lovelace"));

        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);

        var savedPayload = await ReadJsonAsync(saveResponse);
        var savedResume = savedPayload.GetProperty("data");

        Assert.Equal(1, savedResume.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(1, savedResume.GetProperty("revision").GetInt32());
        Assert.Equal(
            "Ada Lovelace",
            savedResume.GetProperty("content").GetProperty("personalInfo").GetProperty("fullName").GetString());

        var getResponse = await client.GetAsync("/api/resume/base");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getPayload = await ReadJsonAsync(getResponse);
        var content = getPayload.GetProperty("data").GetProperty("content");
        Assert.Equal("Builds reliable APIs.", content.GetProperty("summary").GetString());
        Assert.Contains(
            content.GetProperty("skills").EnumerateArray(),
            skill => skill.GetString() == "ASP.NET Core");
    }

    [Fact]
    public async Task SaveBaseResume_ForExistingResume_IncrementsRevision()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        await client.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Ada Lovelace"));

        var updateResponse = await client.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Grace Hopper"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var payload = await ReadJsonAsync(updateResponse);
        var resume = payload.GetProperty("data");
        Assert.Equal(2, resume.GetProperty("revision").GetInt32());
        Assert.Equal(
            "Grace Hopper",
            resume.GetProperty("content").GetProperty("personalInfo").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task GetBaseResume_ForDifferentUser_ReturnsNotFound()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId, OtherUserId);

        var ownerClient = factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);
        await ownerClient.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Ada Lovelace"));

        var otherClient = factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await otherClient.GetAsync("/api/resume/base");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SaveBaseResume_ForDifferentUsers_KeepsResumesIsolated()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId, OtherUserId);

        var ownerClient = factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);
        await ownerClient.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Ada Lovelace"));

        var otherClient = factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);
        await otherClient.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Grace Hopper"));

        var ownerResponse = await ownerClient.GetAsync("/api/resume/base");
        var otherResponse = await otherClient.GetAsync("/api/resume/base");

        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherResponse.StatusCode);

        var ownerPayload = await ReadJsonAsync(ownerResponse);
        var otherPayload = await ReadJsonAsync(otherResponse);

        Assert.Equal(
            "Ada Lovelace",
            ownerPayload.GetProperty("data").GetProperty("content").GetProperty("personalInfo").GetProperty("fullName").GetString());
        Assert.Equal(
            "Grace Hopper",
            otherPayload.GetProperty("data").GetProperty("content").GetProperty("personalInfo").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task SaveBaseResume_WhenUpdatingSkillsSection_PreservesUnrelatedSections()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        await client.PutAsJsonAsync("/api/resume/base", CreateResumeRequest("Ada Lovelace"));

        var response = await client.PutAsJsonAsync(
            "/api/resume/base",
            CreateResumeRequest("Ada Lovelace", skills: new[] { "PostgreSQL", "Redis", "ASP.NET Core" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var content = payload.GetProperty("data").GetProperty("content");
        var skills = content.GetProperty("skills").EnumerateArray().Select(skill => skill.GetString()).ToArray();

        Assert.Equal(new[] { "PostgreSQL", "Redis", "ASP.NET Core" }, skills);
        Assert.Equal("Builds reliable APIs.", content.GetProperty("summary").GetString());
        Assert.Equal(
            "Built API workflows.",
            content.GetProperty("experience")[0].GetProperty("bullets")[0].GetString());
        Assert.Equal(
            "Example University",
            content.GetProperty("education")[0].GetProperty("institution").GetString());
        Assert.Equal(
            "https://example.test/portfolio",
            content.GetProperty("links")[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task SaveBaseResume_WithMalformedContent_ReturnsBadRequest()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.PutAsJsonAsync("/api/resume/base", new
        {
            Content = new
            {
                PersonalInfo = new { FullName = "" },
                Summary = "Builds reliable APIs.",
                Skills = new[] { "ASP.NET Core", "" },
                Experience = Array.Empty<object>(),
                Education = Array.Empty<object>(),
                Certifications = Array.Empty<object>(),
                Links = new[] { new { Label = "Portfolio", Url = "not-a-url" } }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var errors = payload.GetProperty("errors");
        Assert.True(errors.TryGetProperty("PersonalInfo.FullName", out _));
        Assert.True(errors.TryGetProperty("Skills[1]", out _));
        Assert.True(errors.TryGetProperty("Links[0].Url", out _));
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

    private static object CreateResumeRequest(
        string fullName,
        IReadOnlyList<string>? skills = null) => new
    {
        Content = new
        {
            PersonalInfo = new
            {
                FullName = fullName,
                Email = "ada@example.test",
                Phone = "",
                Location = "London",
                Headline = "Backend Engineer",
                Website = "https://example.test"
            },
            Summary = "Builds reliable APIs.",
            Skills = skills ?? new[] { "ASP.NET Core", "PostgreSQL" },
            Experience = new[]
            {
                new
                {
                    Id = "exp-1",
                    Role = "Backend Engineer",
                    Organization = "Example Co",
                    Location = "Remote",
                    StartDate = "2024-01",
                    EndDate = "",
                    IsCurrent = true,
                    Bullets = new[] { "Built API workflows." }
                }
            },
            Education = new[]
            {
                new
                {
                    Id = "edu-1",
                    Institution = "Example University",
                    Degree = "BS",
                    Field = "Computer Science",
                    Location = "",
                    StartDate = "",
                    EndDate = "2022",
                    Details = Array.Empty<string>()
                }
            },
            Certifications = Array.Empty<object>(),
            Links = new[]
            {
                new
                {
                    Id = "link-1",
                    Label = "Portfolio",
                    Url = "https://example.test/portfolio"
                }
            }
        }
    };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload;
    }

    private const string OwnerUserId = "owner-user";
    private const string OtherUserId = "other-user";
}
