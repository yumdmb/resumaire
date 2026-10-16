using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resumaire.Api.Ai;
using Resumaire.Api.Data;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class AiSettingsEndpointsTests
{
    [Fact]
    public async Task ConnectionLifecycle_EncryptsKeys_IsolatesAccounts_AndInvalidatesDiscovery()
    {
        await using var database = new ResumaireApiFactory().WithSqlite();
        var handler = new ProviderHandler();
        using var app = Configure(database, handler);
        await SeedAsync(app);
        var owner = Client(app, "owner");
        var other = Client(app, "other");

        var catalog = await DataAsync(await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "openai", apiKey = "owner-key-1234" }));
        Assert.Contains(catalog.GetProperty("models").EnumerateArray(), model => model.GetProperty("id").GetString() == "whisper-1" && !model.GetProperty("supported").GetBoolean());
        var receipt = await TestAsync(owner, "openai", "gpt-4o-mini", "owner-key-1234");
        var saved = await DataAsync(await owner.PutAsJsonAsync("/api/settings/ai/", new
        {
            providerId = "openai", modelId = "gpt-4o-mini", apiKey = "owner-key-1234", validationToken = receipt
        }));
        Assert.Equal("1234", saved.GetProperty("keyLastFour").GetString());
        Assert.DoesNotContain("owner-key-1234", saved.GetRawText());
        Assert.False((await DataAsync(await other.GetAsync("/api/settings/ai/"))).GetProperty("hasKey").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "openai" })).StatusCode);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().UserAiSettings.SingleAsync();
            Assert.DoesNotContain("owner-key-1234", stored.EncryptedApiKey);
            var protector = scope.ServiceProvider.GetRequiredService<AiCredentialProtector>();
            Assert.Equal("owner-key-1234", protector.Unprotect("owner", stored.EncryptedApiKey));
            Assert.Throws<AiConnectionException>(() => protector.Unprotect("other", stored.EncryptedApiKey));
        }

        await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "openai" });
        var before = handler.CatalogCalls;
        await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "openai" });
        Assert.Equal(before, handler.CatalogCalls);
        var replacement = await TestAsync(owner, "opencode", "gpt-5.4-mini", "zen-key-5678");
        await DataAsync(await owner.PutAsJsonAsync("/api/settings/ai/", new
        {
            providerId = "opencode", modelId = "gpt-5.4-mini", apiKey = "zen-key-5678", validationToken = replacement,
            expectedVersion = saved.GetProperty("credentialVersion").GetGuid()
        }));
        await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "opencode" });
        Assert.Equal(before + 1, handler.CatalogCalls);
        Assert.Contains(handler.Requests, request => request.Host == "opencode.ai" && request.Key == "zen-key-5678");
        Assert.DoesNotContain(handler.Requests, request => request.Host == "opencode.ai" && request.Key == "owner-key-1234");
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync("/api/settings/ai/key")).StatusCode);
        Assert.False((await DataAsync(await owner.GetAsync("/api/settings/ai/"))).GetProperty("hasKey").GetBoolean());
        await using var finalScope = app.Services.CreateAsyncScope();
        Assert.Equal(2, (await finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiUsageBudgets.SingleAsync()).DayRequests);
    }

    [Fact]
    public async Task Receipts_BindUserKeyProviderModel_AndRejectStaleWrites()
    {
        await using var database = new ResumaireApiFactory().WithSqlite();
        var handler = new ProviderHandler();
        using var app = Configure(database, handler);
        await SeedAsync(app);
        var owner = Client(app, "owner");
        var other = Client(app, "other");
        var receipt = await TestAsync(owner, "openai", "gpt-4o-mini", "owner-key-1234");
        foreach (var wrong in new[]
        {
            new { providerId = "openai", modelId = "gpt-4o-mini", apiKey = "changed-key-5678", validationToken = receipt },
            new { providerId = "openai", modelId = "gpt-4.1-mini", apiKey = "owner-key-1234", validationToken = receipt },
            new { providerId = "opencode", modelId = "gpt-5.4-mini", apiKey = "owner-key-1234", validationToken = receipt }
        }) Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync("/api/settings/ai/", wrong)).StatusCode);
        var valid = new { providerId = "openai", modelId = "gpt-4o-mini", apiKey = "owner-key-1234", validationToken = receipt };
        Assert.Equal(HttpStatusCode.BadRequest, (await other.PutAsJsonAsync("/api/settings/ai/", valid)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync("/api/settings/ai/", valid)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync("/api/settings/ai/", valid)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "https://evil.example", apiKey = "owner-key-1234" })).StatusCode);
    }

    [Fact]
    public async Task Discovery_FallsBackOnPermissions_AndNeverEchoesProviderErrors()
    {
        await using var database = new ResumaireApiFactory().WithSqlite();
        var handler = new ProviderHandler { CatalogStatus = HttpStatusCode.Forbidden };
        using var app = Configure(database, handler);
        await SeedAsync(app);
        var owner = Client(app, "owner");
        var fallback = await DataAsync(await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "openai", apiKey = "valid-key-1234" }));
        Assert.True(fallback.GetProperty("isFallback").GetBoolean());
        handler.CatalogStatus = HttpStatusCode.Unauthorized;
        var invalid = await owner.PostAsJsonAsync("/api/settings/ai/models", new { providerId = "openai", apiKey = "invalid-key-5678" });
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
        var body = await invalid.Content.ReadAsStringAsync();
        Assert.Contains("invalid_api_key", body);
        Assert.DoesNotContain("secret-provider-body", body);
        Assert.DoesNotContain("invalid-key-5678", body);
        Assert.Equal("no-store", invalid.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task PersistentQuota_SurvivesNewHost_AndFailedAttemptsConsumeCapacity()
    {
        await using var database = new ResumaireApiFactory().WithSqlite();
        var handler = new ProviderHandler { GenerationStatus = HttpStatusCode.TooManyRequests };
        using (var first = Configure(database, handler, dayLimit: 1))
        {
            await SeedAsync(first);
            var response = await Client(first, "owner").PostAsJsonAsync("/api/settings/ai/test", new { providerId = "openai", modelId = "gpt-4o-mini", apiKey = "owner-key-1234" });
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
        handler.GenerationStatus = HttpStatusCode.OK;
        using var second = Configure(database, handler, dayLimit: 1);
        var limited = await Client(second, "owner").PostAsJsonAsync("/api/settings/ai/test", new { providerId = "openai", modelId = "gpt-4o-mini", apiKey = "replacement-key-5678" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Contains("ai_limit_reached", await limited.Content.ReadAsStringAsync());
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Single(handler.Requests, request => request.Path.EndsWith("responses"));
    }

    [Fact]
    public async Task ActiveLease_RejectsDuplicatePaidRequest_AndAccountsUseSeparateCredentials()
    {
        await using var database = new ResumaireApiFactory().WithSqlite();
        var handler = new ProviderHandler { Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var app = Configure(database, handler);
        await SeedAsync(app);
        var owner = Client(app, "owner");
        var first = owner.PostAsJsonAsync("/api/settings/ai/test", new { providerId = "openai", modelId = "gpt-4o-mini", apiKey = "owner-key-1234" });
        await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var duplicate = await owner.PostAsJsonAsync("/api/settings/ai/test", new { providerId = "openai", modelId = "gpt-4o-mini", apiKey = "owner-key-1234" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("ai_request_active", await duplicate.Content.ReadAsStringAsync());
        var other = Client(app, "other").PostAsJsonAsync("/api/settings/ai/test", new { providerId = "opencode", modelId = "gpt-5.4-mini", apiKey = "other-key-5678" });
        handler.Gate.SetResult();
        Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other).StatusCode);
        Assert.Equal(2, handler.Requests.Count(request => request.Path.EndsWith("responses")));
        Assert.Contains(handler.Requests, request => request.Host == "api.openai.com" && request.Key == "owner-key-1234");
        Assert.Contains(handler.Requests, request => request.Host == "opencode.ai" && request.Key == "other-key-5678");
    }

    [Fact]
    public async Task ContributorModel_RequiresAcknowledgement_AndAnonymousAccessIsDenied()
    {
        await using var database = new ResumaireApiFactory().WithSqlite();
        var handler = new ProviderHandler();
        using var app = Configure(database, handler);
        await SeedAsync(app);
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CreateClient().GetAsync("/api/settings/ai/")).StatusCode);
        var response = await Client(app, "owner").PostAsJsonAsync("/api/settings/ai/test", new { providerId = "opencode", modelId = "muse-spark-1.3-contributor-free", apiKey = "owner-key-1234" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(handler.Requests);
    }

    private static WebApplicationFactory<Program> Configure(ResumaireApiFactory factory, ProviderHandler handler, int dayLimit = 50) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AiTailoring:RequestsPerMinute", "50");
            builder.UseSetting("AiTailoring:RequestsPerDay", dayLimit.ToString());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiProviderClient>();
                services.AddSingleton<IAiProviderClient>(provider => new AiProviderClient(new HttpClient(handler, disposeHandler: false),
                    provider.GetRequiredService<AiProviderRegistry>(), provider.GetRequiredService<IMemoryCache>()));
            });
        });

    private static async Task SeedAsync(WebApplicationFactory<Program> app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Users.AddRange(new ApplicationUser { Id = "owner", UserName = "owner@example.test" }, new ApplicationUser { Id = "other", UserName = "other@example.test" });
        await db.SaveChangesAsync();
    }
    private static HttpClient Client(WebApplicationFactory<Program> app, string userId)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, userId);
        return client;
    }
    private static async Task<JsonElement> DataAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }
    private static async Task<string> TestAsync(HttpClient client, string provider, string model, string key) =>
        (await DataAsync(await client.PostAsJsonAsync("/api/settings/ai/test", new { providerId = provider, modelId = model, apiKey = key })))
            .GetProperty("validationToken").GetString()!;

    private sealed class ProviderHandler : HttpMessageHandler
    {
        public ConcurrentQueue<(string Host, string Path, string Key)> Requests { get; } = new();
        public int CatalogCalls;
        public HttpStatusCode CatalogStatus { get; set; } = HttpStatusCode.OK;
        public HttpStatusCode GenerationStatus { get; set; } = HttpStatusCode.OK;
        public TaskCompletionSource? Gate { get; init; }
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Enqueue((request.RequestUri!.Host, request.RequestUri.AbsolutePath, request.Headers.Authorization!.Parameter!));
            var discovery = request.Method == HttpMethod.Get;
            if (discovery) Interlocked.Increment(ref CatalogCalls);
            else
            {
                Entered.TrySetResult();
                if (Gate is not null) await Gate.Task.WaitAsync(cancellationToken);
            }
            var status = discovery ? CatalogStatus : GenerationStatus;
            var json = status != HttpStatusCode.OK ? "secret-provider-body" : discovery
                ? """{"data":[{"id":"gpt-4o-mini"},{"id":"gpt-5.4-mini"},{"id":"whisper-1"}]}"""
                : """{"output_text":"{\"ok\":true}","usage":{"input_tokens":10,"output_tokens":5}}""";
            return new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
