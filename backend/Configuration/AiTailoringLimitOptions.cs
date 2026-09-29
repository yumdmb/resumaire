namespace Resumaire.Api.Configuration;

/// <summary>Per-user limits on the paid suggestion-generation endpoint.</summary>
public sealed class AiTailoringLimitOptions
{
    public const string SectionName = "AiTailoring";

    public int RequestsPerMinute { get; set; } = 5;

    public int RequestsPerDay { get; set; } = 50;
}
