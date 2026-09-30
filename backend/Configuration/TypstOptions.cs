namespace Resumaire.Api.Configuration;

public sealed class TypstOptions
{
    public const string SectionName = "Typst";

    /// <summary>Typst executable name or absolute path.</summary>
    public string Executable { get; set; } = "typst";

    public int TimeoutSeconds { get; set; } = 20;

    public int MaxConcurrentRenders { get; set; } = 2;

    public int CacheMinutes { get; set; } = 10;
}
