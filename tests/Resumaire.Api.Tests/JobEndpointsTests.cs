using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class JobEndpointsTests
{
    [Fact]
    public async Task CreateGetUpdateDeleteJob_ForOwner_CompletesCrudFlow()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var createResponse = await client.PostAsJsonAsync("/api/jobs", new
        {
            Company = " Example Co ",
            Title = " Senior Backend Engineer ",
            Link = "https://example.com/jobs/backend",
            Description = "Build APIs.",
            Status = "Applied",
            DateApplied = "2026-05-12",
            Notes = " Referred by team. ",
            SelectedBaseResumeId = (Guid?)null,
            SelectedTailoredResumeId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdPayload = await ReadJsonAsync(createResponse);
        var createdJob = createdPayload.GetProperty("data");
        var jobId = createdJob.GetProperty("id").GetGuid();
        Assert.Equal("Example Co", createdJob.GetProperty("company").GetString());
        Assert.Equal("Senior Backend Engineer", createdJob.GetProperty("title").GetString());
        Assert.Equal("Applied", createdJob.GetProperty("status").GetString());
        Assert.Equal("Referred by team.", createdJob.GetProperty("notes").GetString());

        var getResponse = await client.GetAsync($"/api/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var detailPayload = await ReadJsonAsync(getResponse);
        Assert.Equal("Build APIs.", detailPayload.GetProperty("data").GetProperty("description").GetString());

        var updateResponse = await client.PutAsJsonAsync($"/api/jobs/{jobId}", new
        {
            Company = "Example Co",
            Title = "Staff Backend Engineer",
            Link = "https://example.com/jobs/staff-backend",
            Description = "Own the API platform.",
            Status = "Interview",
            DateApplied = "2026-05-12",
            Notes = "Phone screen scheduled.",
            SelectedBaseResumeId = (Guid?)null,
            SelectedTailoredResumeId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedPayload = await ReadJsonAsync(updateResponse);
        var updatedJob = updatedPayload.GetProperty("data");
        Assert.Equal("Staff Backend Engineer", updatedJob.GetProperty("title").GetString());
        Assert.Equal("Interview", updatedJob.GetProperty("status").GetString());
        Assert.Equal("Phone screen scheduled.", updatedJob.GetProperty("notes").GetString());

        var deleteResponse = await client.DeleteAsync($"/api/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getDeletedResponse = await client.GetAsync($"/api/jobs/{jobId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task JobEndpoints_ForDifferentUser_ReturnNotFoundAndDoNotExposeData()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var ownerJobId = await SeedJobAsync(factory, OwnerUserId, JobStatus.Saved);
        await SeedUsersAsync(factory, OtherUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var getResponse = await client.GetAsync($"/api/jobs/{ownerJobId}");
        var updateResponse = await client.PutAsJsonAsync($"/api/jobs/{ownerJobId}", new
        {
            Company = "Other Co",
            Title = "Other Title",
            Link = (string?)null,
            Description = "Attempt to overwrite another user's job.",
            Status = "Offer",
            DateApplied = (string?)null,
            Notes = (string?)null,
            SelectedBaseResumeId = (Guid?)null,
            SelectedTailoredResumeId = (Guid?)null
        });
        var deleteResponse = await client.DeleteAsync($"/api/jobs/{ownerJobId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ownerJob = await dbContext.Jobs.AsNoTracking().SingleAsync(job => job.Id == ownerJobId);
        Assert.Equal(OwnerUserId, ownerJob.UserId);
        Assert.Equal(JobStatus.Saved, ownerJob.Status);
    }

    [Fact]
    public async Task ListJobs_WithStatusFilter_ReturnsOnlyMatchingOwnedJobs()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var interviewJobId = await SeedJobAsync(factory, OwnerUserId, JobStatus.Interview, title: "Interview role");
        await SeedJobAsync(factory, OwnerUserId, JobStatus.Applied, title: "Applied role");
        await SeedJobAsync(factory, OtherUserId, JobStatus.Interview, title: "Other user's interview");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/jobs?status=Interview");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());

        var payload = await ReadJsonAsync(response);
        var jobs = payload.GetProperty("data").EnumerateArray().ToArray();
        var job = Assert.Single(jobs);

        Assert.Equal(interviewJobId, job.GetProperty("id").GetGuid());
        Assert.Equal("Interview role", job.GetProperty("title").GetString());
        Assert.Equal("Interview", job.GetProperty("status").GetString());
    }

    [Fact]
    public async Task PatchJobStatus_ForOwner_UpdatesOnlyStatusAndUpdatedAt()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var createdAt = DateTimeOffset.UtcNow.AddDays(-3);
        var originalUpdatedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var dateApplied = new DateOnly(2026, 5, 12);
        var jobId = await SeedJobAsync(
            factory,
            OwnerUserId,
            JobStatus.Saved,
            company: "Original Co",
            title: "Backend Engineer",
            description: "Original description.",
            link: "https://example.com/jobs/backend",
            dateApplied: dateApplied,
            notes: "Original notes.",
            createdAt: createdAt,
            updatedAt: originalUpdatedAt);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await PatchJobStatusAsync(client, jobId, "Interview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var updatedJob = payload.GetProperty("data");
        Assert.Equal(jobId, updatedJob.GetProperty("id").GetGuid());
        Assert.Equal("Interview", updatedJob.GetProperty("status").GetString());
        Assert.Equal("Original Co", updatedJob.GetProperty("company").GetString());
        Assert.Equal("Backend Engineer", updatedJob.GetProperty("title").GetString());
        Assert.False(updatedJob.TryGetProperty("description", out _));
        Assert.True(updatedJob.GetProperty("updatedAt").GetDateTimeOffset() > originalUpdatedAt);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedJob = await dbContext.Jobs.AsNoTracking().SingleAsync(job => job.Id == jobId);

        Assert.Equal(JobStatus.Interview, storedJob.Status);
        Assert.Equal("Original Co", storedJob.Company);
        Assert.Equal("Backend Engineer", storedJob.Title);
        Assert.Equal("Original description.", storedJob.Description);
        Assert.Equal("https://example.com/jobs/backend", storedJob.Link);
        Assert.Equal(dateApplied, storedJob.DateApplied);
        Assert.Equal("Original notes.", storedJob.Notes);
        Assert.Equal(createdAt, storedJob.CreatedAt);
        Assert.True(storedJob.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task PatchJobStatus_WithUnsupportedStatus_ReturnsBadRequest()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var jobId = await SeedJobAsync(factory, OwnerUserId, JobStatus.Saved);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await PatchJobStatusAsync(client, jobId, "Screening");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var statusErrors = payload.GetProperty("errors").GetProperty("Status").EnumerateArray().ToArray();
        Assert.Contains(statusErrors, error => error.GetString() == SupportedStatusMessage);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedJob = await dbContext.Jobs.AsNoTracking().SingleAsync(job => job.Id == jobId);
        Assert.Equal(JobStatus.Saved, storedJob.Status);
    }

    [Fact]
    public async Task PatchJobStatus_ForDifferentUser_ReturnsNotFoundAndDoesNotExposeData()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        var originalUpdatedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var ownerJobId = await SeedJobAsync(
            factory,
            OwnerUserId,
            JobStatus.Saved,
            updatedAt: originalUpdatedAt);
        await SeedUsersAsync(factory, OtherUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OtherUserId);

        var response = await PatchJobStatusAsync(client, ownerJobId, "Offer");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ownerJob = await dbContext.Jobs.AsNoTracking().SingleAsync(job => job.Id == ownerJobId);
        Assert.Equal(OwnerUserId, ownerJob.UserId);
        Assert.Equal(JobStatus.Saved, ownerJob.Status);
        Assert.Equal(originalUpdatedAt, ownerJob.UpdatedAt);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task SaveJob_WithUnsupportedStatus_ReturnsBadRequest(string method)
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);
        var jobId = method == "PUT"
            ? await SeedJobAsync(factory, OwnerUserId, JobStatus.Saved)
            : Guid.NewGuid();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var request = new
        {
            Company = "Example Co",
            Title = "Backend Engineer",
            Link = (string?)null,
            Description = "Build APIs.",
            Status = "Screening",
            DateApplied = (string?)null,
            Notes = (string?)null,
            SelectedBaseResumeId = (Guid?)null,
            SelectedTailoredResumeId = (Guid?)null
        };

        var response = method == "POST"
            ? await client.PostAsJsonAsync("/api/jobs", request)
            : await client.PutAsJsonAsync($"/api/jobs/{jobId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var statusErrors = payload.GetProperty("errors").GetProperty("Status").EnumerateArray().ToArray();
        Assert.Contains(statusErrors, error => error.GetString() == SupportedStatusMessage);
    }

    [Fact]
    public async Task ListJobs_WithUnsupportedStatusFilter_ReturnsBadRequest()
    {
        await using var factory = await CreateMigratedFactoryAsync();
        await ResetDatabaseAsync(factory);
        await SeedUsersAsync(factory, OwnerUserId);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, OwnerUserId);

        var response = await client.GetAsync("/api/jobs?status=Screening");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        var statusErrors = payload.GetProperty("errors").GetProperty("status").EnumerateArray().ToArray();
        Assert.Contains(statusErrors, error => error.GetString() == SupportedStatusMessage);
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

    private static async Task<Guid> SeedJobAsync(
        ResumaireApiFactory factory,
        string userId,
        JobStatus status,
        string company = "Example Co",
        string title = "Backend Engineer",
        string description = "Build APIs.",
        string? link = null,
        DateOnly? dateApplied = null,
        string? notes = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        await SeedUsersAsync(factory, userId);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var job = new Job
        {
            UserId = userId,
            Company = company,
            Title = title,
            Link = link,
            Description = description,
            Status = status,
            DateApplied = dateApplied,
            Notes = notes,
            CreatedAt = createdAt ?? now,
            UpdatedAt = updatedAt ?? now
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync();

        return job.Id;
    }

    private static async Task<HttpResponseMessage> PatchJobStatusAsync(
        HttpClient client,
        Guid jobId,
        string? status)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/jobs/{jobId}/status")
        {
            Content = JsonContent.Create(new { Status = status })
        };

        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload;
    }

    private const string OwnerUserId = "owner-user";
    private const string OtherUserId = "other-user";
    private const string SupportedStatusMessage =
        "Status must be one of: Saved, Applied, Interview, Rejected, Offer.";
}
