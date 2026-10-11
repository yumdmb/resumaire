namespace Resumaire.Api.Data.Entities;

public sealed class AiUsageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public required string ProviderId { get; set; }
    public required string ModelId { get; set; }
    public required string Operation { get; set; }
    public string Status { get; set; } = "Started";
    public DateTimeOffset StartedAt { get; set; }
    public int DurationMilliseconds { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}
