using Microsoft.Extensions.Configuration;
using Resumaire.Api.Configuration;

namespace Resumaire.Api.Tests;

public sealed class OpenAiOptionsBindingTests
{
    [Fact]
    public void Bind_WithoutTemperatureSetting_UsesDefaults()
    {
        var options = Bind([]);

        Assert.Equal(0.2, options.Temperature);
        Assert.Equal(4000, options.MaxOutputTokens);
        Assert.Equal(60, options.TimeoutSeconds);
        Assert.Equal("gpt-4o-mini", options.EffectiveModel);
    }

    [Fact]
    public void Bind_WithEmptyTemperature_OmitsTemperatureForReasoningModels()
    {
        var options = Bind(new Dictionary<string, string?> { ["OpenAI:Temperature"] = "" });

        Assert.Null(options.Temperature);
    }

    [Fact]
    public void Bind_WithExplicitValues_UsesThem()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["OpenAI:Temperature"] = "0.7",
            ["OpenAI:MaxOutputTokens"] = "8000",
            ["OpenAI:Model"] = "  "
        });

        Assert.Equal(0.7, options.Temperature);
        Assert.Equal(8000, options.MaxOutputTokens);
        Assert.Equal("gpt-4o-mini", options.EffectiveModel);
    }

    private static OpenAiOptions Bind(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var options = new OpenAiOptions();
        configuration.GetSection(OpenAiOptions.SectionName).Bind(options);
        return options;
    }
}
