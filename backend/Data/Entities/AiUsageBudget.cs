namespace Resumaire.Api.Data.Entities;

// Separate from credentials: deleting a connection must never reset a user's limits.
public sealed class AiUsageBudget
{
    public required string UserId { get; set; }
    public long MinuteWindow { get; set; }
    public int MinuteRequests { get; set; }
    public long DayWindow { get; set; }
    public int DayRequests { get; set; }
    public Guid? ActiveRequestId { get; set; }
    public long ActiveUntil { get; set; }
}
