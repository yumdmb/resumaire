namespace Resumaire.Api.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public const string DefaultModel = "gpt-4o-mini";

    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

    public string Model { get; set; } = DefaultModel;

    /// <summary>
    /// Sampling temperature. Set it to an empty value to omit it, which reasoning models
    /// (o-series, gpt-5, gpt-oss) require because they reject the parameter.
    /// </summary>
    public double? Temperature { get; set; } = 0.2;

    /// <summary>Output token cap. Reasoning models count thinking tokens against it, so raise it for them.</summary>
    public int MaxOutputTokens { get; set; } = 4000;

    public int TimeoutSeconds { get; set; } = 60;

    public string EffectiveModel => string.IsNullOrWhiteSpace(Model) ? DefaultModel : Model;
}
