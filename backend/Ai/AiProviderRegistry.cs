namespace Resumaire.Api.Ai;

public sealed record AiModel(string Id, string Name, bool Supported, bool Recommended, bool UsesTrainingData, string? Note);
public sealed record AiProvider(string Id, string Name, string BaseUrl, string PrivacyUrl, IReadOnlyList<AiModel> Models);

public sealed class AiProviderRegistry
{
    public IReadOnlyList<AiProvider> Providers { get; } =
    [
        new("openai", "OpenAI", "https://api.openai.com/v1/", "https://developers.openai.com/api/docs/guides/your-data",
        [
            Model("gpt-4o-mini", "GPT-4o mini", recommended: true),
            Model("gpt-4.1-mini", "GPT-4.1 mini"), Model("gpt-4.1", "GPT-4.1"), Model("gpt-4o", "GPT-4o"),
            Model("gpt-5-mini", "GPT-5 mini"), Model("gpt-5", "GPT-5"),
            Model("gpt-5.4-mini", "GPT-5.4 mini"), Model("gpt-5.4", "GPT-5.4")
        ]),
        new("opencode", "OpenCode Zen", "https://opencode.ai/zen/v1/", "https://docs.opencode.ai/docs/zen/#privacy",
        [
            Model("gpt-5.4-mini", "GPT-5.4 mini", recommended: true), Model("gpt-5.4", "GPT-5.4"),
            Model("gpt-5.5", "GPT-5.5"), Model("muse-spark-1.3", "Muse Spark 1.3"),
            new("muse-spark-1.3-contributor-free", "Muse Spark 1.3 Contributor Free", true, false, true,
                "Prompts and responses may be used to train future Meta models. Free availability is temporary.")
        ])
    ];

    public AiProvider Get(string? id) => Providers.FirstOrDefault(provider => provider.Id == id)
        ?? throw new AiConnectionException(400, "unsupported_provider", "Choose a supported AI provider.");

    public AiModel GetModel(string providerId, string? modelId)
    {
        var provider = Get(providerId);
        var model = provider.Models.FirstOrDefault(model => model.Id == modelId);
        if (model is not null) return model;
        // OpenAI versioned snapshots inherit the supported alias's protocol, not arbitrary name prefixes.
        if (providerId == "openai" && modelId is not null)
        {
            var alias = provider.Models.FirstOrDefault(model => modelId.StartsWith(model.Id + "-", StringComparison.Ordinal)
                && DateOnly.TryParseExact(modelId[(model.Id.Length + 1)..], "yyyy-MM-dd", out _));
            if (alias is not null) return alias with { Id = modelId, Name = modelId, Recommended = false };
        }
        throw new AiConnectionException(400, "unsupported_model", "This model is not supported for resume tailoring. Choose a supported model.");
    }

    public AiModel Describe(string providerId, string modelId)
    {
        try { return GetModel(providerId, modelId); }
        catch (AiConnectionException) { return new(modelId, modelId, false, false, false, "Not yet supported for resume tailoring."); }
    }

    private static AiModel Model(string id, string name, bool recommended = false) =>
        new(id, name, true, recommended, false, null);
}

public sealed class AiConnectionException(int status, string code, string message, int? retryAfter = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public int? RetryAfter { get; } = retryAfter;
}

// Do not use a record: its generated ToString would expose the key.
public sealed class AiConnection(AiProvider provider, AiModel model, string apiKey)
{
    public AiProvider Provider { get; } = provider;
    public AiModel Model { get; } = model;
    public string ApiKey { get; } = apiKey;
}
