using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Resumaire.Api.Ai;
using Resumaire.Api.Data;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class PersistentAiUsageTests(PostgresTestDatabase database) : IClassFixture<PostgresTestDatabase>
{
    [Fact]
    public async Task Postgres_ReservesOnlyOneConcurrentRequest_AndKeepsDailyQuotaAcrossScopes()
    {
        await using var app = new ResumaireApiFactory().WithDatabase(database.ConnectionString);
        await app.MigrateDatabaseAsync();
        var userId = Guid.NewGuid().ToString();
        await using (var seed = app.Services.CreateAsyncScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new ApplicationUser { Id = userId, UserName = userId });
            // Pre-create the row so this test concentrates on the atomic multi-instance reservation.
            db.AiUsageBudgets.Add(new() { UserId = userId });
            await db.SaveChangesAsync();
        }
        var registry = new AiProviderRegistry();
        var connection = new AiConnection(registry.Get("openai"), registry.GetModel("openai", "gpt-4o-mini"), "synthetic-key");
        async Task<Resumaire.Api.Data.Entities.AiUsageRecord?> Reserve()
        {
            await using var scope = app.Services.CreateAsyncScope();
            try { return await scope.ServiceProvider.GetRequiredService<AiUsageService>().ReserveAsync(userId, connection, "Test", CancellationToken.None); }
            catch (AiConnectionException exception) { Assert.Equal("ai_request_active", exception.Code); return null; }
        }
        var results = await Task.WhenAll(Reserve(), Reserve());
        var winner = Assert.Single(results, record => record is not null)!;
        await using (var finish = app.Services.CreateAsyncScope())
        {
            var db = finish.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, (await db.AiUsageBudgets.SingleAsync(value => value.UserId == userId)).DayRequests);
            await finish.ServiceProvider.GetRequiredService<AiUsageService>().FinishAsync(winner, "Succeeded", 10, 5);
            // Simulate reaching the daily limit independently of the connection or process lifetime.
            await db.AiUsageBudgets.Where(value => value.UserId == userId).ExecuteUpdateAsync(update => update.SetProperty(value => value.DayRequests, 50));
        }
        await using var restarted = app.Services.CreateAsyncScope();
        var limited = await Assert.ThrowsAsync<AiConnectionException>(() => restarted.ServiceProvider.GetRequiredService<AiUsageService>()
            .ReserveAsync(userId, connection, "Tailoring", CancellationToken.None));
        Assert.Equal("ai_limit_reached", limited.Code);
        var usage = await restarted.ServiceProvider.GetRequiredService<ApplicationDbContext>().AiUsageRecords.SingleAsync(value => value.UserId == userId);
        Assert.Equal("Succeeded", usage.Status);
        Assert.Equal(10, usage.InputTokens);
    }
}
