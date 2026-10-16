using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resumaire.Api.Configuration;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;

namespace Resumaire.Api.Ai;

public sealed class AiUsageService(ApplicationDbContext db, IOptions<AiTailoringLimitOptions> options, TimeProvider clock)
{
    public async Task<AiUsageRecord> ReserveAsync(string userId, AiConnection connection, string operation, CancellationToken cancellationToken)
    {
        if (!await db.AiUsageBudgets.AnyAsync(value => value.UserId == userId, cancellationToken))
        {
            var budget = new AiUsageBudget { UserId = userId };
            db.AiUsageBudgets.Add(budget);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException)
            {
                db.Entry(budget).State = EntityState.Detached;
                if (!await db.AiUsageBudgets.AnyAsync(value => value.UserId == userId, cancellationToken)) throw;
            }
        }
        var now = clock.GetUtcNow();
        var seconds = now.ToUnixTimeSeconds();
        var minute = seconds / 60;
        var day = seconds / 86400;
        var limits = options.Value;
        var record = new AiUsageRecord { UserId = userId, ProviderId = connection.Provider.Id, ModelId = connection.Model.Id, Operation = operation, StartedAt = now };
        var reserved = await db.AiUsageBudgets.Where(value => value.UserId == userId
                && (value.ActiveRequestId == null || value.ActiveUntil <= seconds)
                && (value.MinuteWindow != minute || value.MinuteRequests < limits.RequestsPerMinute)
                && (value.DayWindow != day || value.DayRequests < limits.RequestsPerDay))
            .ExecuteUpdateAsync(update => update
                .SetProperty(value => value.MinuteRequests, value => value.MinuteWindow == minute ? value.MinuteRequests + 1 : 1)
                .SetProperty(value => value.MinuteWindow, minute)
                .SetProperty(value => value.DayRequests, value => value.DayWindow == day ? value.DayRequests + 1 : 1)
                .SetProperty(value => value.DayWindow, day)
                .SetProperty(value => value.ActiveRequestId, record.Id)
                .SetProperty(value => value.ActiveUntil, seconds + 150), cancellationToken);
        if (reserved == 0)
        {
            var budget = await db.AiUsageBudgets.AsNoTracking().SingleAsync(value => value.UserId == userId, cancellationToken);
            if (budget.ActiveRequestId is not null && budget.ActiveUntil > seconds)
                throw new AiConnectionException(409, "ai_request_active", "An AI request is already running. Wait for it to finish before trying again.", (int)(budget.ActiveUntil - seconds));
            var retry = budget.DayWindow == day && budget.DayRequests >= limits.RequestsPerDay ? (day + 1) * 86400 - seconds : (minute + 1) * 60 - seconds;
            throw new AiConnectionException(429, "ai_limit_reached", "You have reached your AI request limit. Try again after the limit resets.", (int)retry);
        }
        try
        {
            db.AiUsageRecords.Add(record);
            await db.SaveChangesAsync(cancellationToken);
            return record;
        }
        catch
        {
            await ReleaseAsync(userId, record.Id);
            throw;
        }
    }

    public async Task FinishAsync(AiUsageRecord record, string status, int? inputTokens = null, int? outputTokens = null)
    {
        try
        {
            await db.AiUsageRecords.Where(value => value.Id == record.Id).ExecuteUpdateAsync(update => update
                .SetProperty(value => value.Status, status)
                .SetProperty(value => value.DurationMilliseconds, (int)Math.Clamp((clock.GetUtcNow() - record.StartedAt).TotalMilliseconds, 0, int.MaxValue))
                .SetProperty(value => value.InputTokens, inputTokens).SetProperty(value => value.OutputTokens, outputTokens), CancellationToken.None);
        }
        finally { await ReleaseAsync(record.UserId, record.Id); }
    }

    private Task<int> ReleaseAsync(string userId, Guid requestId) => db.AiUsageBudgets
        .Where(value => value.UserId == userId && value.ActiveRequestId == requestId)
        .ExecuteUpdateAsync(update => update.SetProperty(value => value.ActiveRequestId, (Guid?)null).SetProperty(value => value.ActiveUntil, 0L), CancellationToken.None);
}
