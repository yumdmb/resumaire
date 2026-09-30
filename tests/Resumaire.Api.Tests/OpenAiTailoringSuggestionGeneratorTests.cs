using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Resumaire.Api.Configuration;
using Resumaire.Api.Contracts;
using Resumaire.Api.Tailoring;

namespace Resumaire.Api.Tests;

public sealed class OpenAiTailoringSuggestionGeneratorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GenerateAsync_WhenProviderReturnsFencedJson_ParsesStructuredOutput()
    {
        var output = """
            ```json
            {
              "suggestions": [
                {
                  "targetSection": "Experience",
                  "originalContent": "Built a React dashboard.",
                  "suggestedContent": "Built a React dashboard for API workflow review.",
                  "rationale": "The job emphasizes React and this bullet already supports it.",
                  "sourceEvidencePaths": ["Experience[0].Bullets[0]"],
                  "aiNotes": ""
                }
              ],
              "gapNotes": [
                {
                  "keyword": "Docker",
                  "reason": "Docker is not supported by the base resume evidence."
                }
              ]
            }
            ```
            """;
        using var httpClient = new HttpClient(new StubResponsesHandler(output))
        {
            BaseAddress = new Uri("https://openrouter.ai/api/v1/")
        };
        var generator = new OpenAiTailoringSuggestionGenerator(
            httpClient,
            Options.Create(new OpenAiOptions
            {
                ApiKey = "test-key",
                Model = "openai/gpt-oss-120b:free"
            }),
            NullLogger<OpenAiTailoringSuggestionGenerator>.Instance);

        var result = await generator.GenerateAsync(CreateRequest(), CancellationToken.None);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal("Experience", suggestion.TargetSection);
        Assert.Equal("Built a React dashboard for API workflow review.", suggestion.SuggestedContent);
        Assert.Equal(["Experience[0].Bullets[0]"], suggestion.SourceEvidencePaths);

        var gapNote = Assert.Single(result.GapNotes);
        Assert.Equal("Docker", gapNote.Keyword);
    }

    [Fact]
    public async Task GenerateAsync_ParsesTargetPathAndOperation()
    {
        var output = """
            {"suggestions":[{"targetSection":"Experience","targetPath":"Experience[0].Bullets[0]","operation":"Replace",
            "originalContent":"","suggestedContent":"Built a React dashboard.","rationale":"Grounded.",
            "sourceEvidencePaths":["Experience[0].Bullets[0]"],"aiNotes":""}],"gapNotes":[]}
            """;

        var result = await CreateGenerator(new StubResponsesHandler(output)).GenerateAsync(CreateRequest(), CancellationToken.None);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal("Experience[0].Bullets[0]", suggestion.TargetPath);
        Assert.Equal(TailoringOperations.Replace, suggestion.Operation);
    }

    [Fact]
    public async Task GenerateAsync_WhenProviderOmitsFields_DoesNotThrowNullReference()
    {
        var output = """{"suggestions":[{"targetSection":"Summary"}],"gapNotes":null}""";

        var result = await CreateGenerator(new StubResponsesHandler(output)).GenerateAsync(CreateRequest(), CancellationToken.None);

        var suggestion = Assert.Single(result.Suggestions);
        Assert.Equal(string.Empty, suggestion.SuggestedContent);
        Assert.Empty(suggestion.SourceEvidencePaths);
        Assert.Empty(result.GapNotes);
    }

    [Fact]
    public async Task GenerateAsync_WhenSuggestionsMissing_ReturnsNoSuggestions()
    {
        var result = await CreateGenerator(new StubResponsesHandler("{}")).GenerateAsync(CreateRequest(), CancellationToken.None);

        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public async Task GenerateAsync_WhenApiKeyMissing_ThrowsUnavailable()
    {
        var generator = CreateGenerator(new StubResponsesHandler("{}"), new OpenAiOptions { ApiKey = null });

        await Assert.ThrowsAsync<AiTailoringUnavailableException>(
            () => generator.GenerateAsync(CreateRequest(), CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "API key")]
    [InlineData(HttpStatusCode.TooManyRequests, "rate limit")]
    [InlineData(HttpStatusCode.BadGateway, "unavailable")]
    [InlineData(HttpStatusCode.BadRequest, "failed")]
    public async Task GenerateAsync_WhenProviderReturnsError_ThrowsProviderExceptionWithReason(
        HttpStatusCode status,
        string expectedFragment)
    {
        var handler = new StubResponsesHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("secret resume text echoed by provider")
        });

        var exception = await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));

        Assert.Contains(expectedFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret resume text", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_WhenProviderReturnsNonJson_ThrowsProviderException()
    {
        var handler = new StubResponsesHandler(_ => StubResponsesHandler.Ok("<html>gateway error</html>"));

        await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_WhenOutputIsTruncated_ExplainsTokenLimit()
    {
        var handler = new StubResponsesHandler(_ => StubResponsesHandler.Ok(
            """{"status":"incomplete","incomplete_details":{"reason":"max_output_tokens"},"output":[]}"""));

        var exception = await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));

        Assert.Contains("MaxOutputTokens", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_WhenModelRefuses_ThrowsProviderException()
    {
        var handler = new StubResponsesHandler(_ => StubResponsesHandler.Ok(
            """{"output":[{"content":[{"type":"refusal","refusal":"No."}]}]}"""));

        var exception = await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));

        Assert.Contains("refused", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_WhenNetworkFails_ThrowsProviderException()
    {
        var handler = new StubResponsesHandler(_ => throw new HttpRequestException("connection reset"));

        await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_WhenRequestTimesOut_ThrowsProviderException()
    {
        var handler = new StubResponsesHandler(_ => throw new TaskCanceledException("timeout"));

        var exception = await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));

        Assert.Contains("timed out", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new StubResponsesHandler(_ =>
        {
            cancellation.Cancel();
            throw new TaskCanceledException("canceled", null, cancellation.Token);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), cancellation.Token));
    }

    [Fact]
    public async Task GenerateAsync_OmitsTemperatureWhenNotConfigured_AndSendsConfiguredTokenLimit()
    {
        var handler = new StubResponsesHandler("""{"suggestions":[],"gapNotes":[]}""");
        var generator = CreateGenerator(
            handler,
            new OpenAiOptions { ApiKey = "test-key", Temperature = null, MaxOutputTokens = 6000 });

        await generator.GenerateAsync(CreateRequest(), CancellationToken.None);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.False(body.RootElement.TryGetProperty("temperature", out _));
        Assert.Equal(6000, body.RootElement.GetProperty("max_output_tokens").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_SendsDefaultTemperature()
    {
        var handler = new StubResponsesHandler("""{"suggestions":[],"gapNotes":[]}""");

        await CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(0.2, body.RootElement.GetProperty("temperature").GetDouble());
    }

    [Theory]
    [InlineData("Here is the result:\n```json\n{\"suggestions\":[],\"gapNotes\":[]}\n```")]
    [InlineData("```json {\"suggestions\":[],\"gapNotes\":[]} ```")]
    public async Task GenerateAsync_WhenFenceHasLeadingTextOrNoNewline_FailsClearlyInsteadOfCrashing(string output)
    {
        // Documented limit: only output that starts with a fence is normalized; anything else is a clean provider error.
        var handler = new StubResponsesHandler(output);

        await Assert.ThrowsAsync<AiTailoringProviderException>(
            () => CreateGenerator(handler).GenerateAsync(CreateRequest(), CancellationToken.None));
    }

    private static OpenAiTailoringSuggestionGenerator CreateGenerator(
        HttpMessageHandler handler,
        OpenAiOptions? options = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") },
            Options.Create(options ?? new OpenAiOptions { ApiKey = "test-key" }),
            NullLogger<OpenAiTailoringSuggestionGenerator>.Instance);

    private static AiTailoringSuggestionGenerationRequest CreateRequest()
    {
        var resumeContent = new ResumeContentDto(
            PersonalInfo: null,
            Summary: "Builds reliable APIs.",
            Skills: ["React"],
            Experience:
            [
                new ResumeExperienceDto(
                    "exp-1",
                    "Engineer",
                    "Example Co",
                    null,
                    null,
                    null,
                    true,
                    ["Built a React dashboard."])
            ],
            Education: [],
            Certifications: [],
            Links: []);

        var reactKeyword = new JobKeyword("React", KeywordCategories.Framework, ["react"], 1);
        var comparison = new ResumeKeywordComparisonResult(
            [
                new SupportedResumeKeyword(
                    reactKeyword,
                    [new ResumeKeywordEvidence("Experience", "Experience[0].Bullets[0]", "Built a React dashboard.")])
            ],
            [new MissingResumeKeyword(new JobKeyword("Docker", KeywordCategories.Tool, ["docker"], 1))],
            []);

        return new AiTailoringSuggestionGenerationRequest(
            "Full Stack Engineer",
            "Example Co",
            "Build React features and Docker deployments.",
            resumeContent,
            [reactKeyword],
            comparison);
    }

    private sealed class StubResponsesHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubResponsesHandler(string outputText)
            : this(_ => Ok(JsonSerializer.Serialize(new
            {
                output = new[]
                {
                    new { content = new[] { new { type = "output_text", text = outputText } } }
                }
            }, JsonOptions)))
        {
        }

        public StubResponsesHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public string? LastRequestBody { get; private set; }

        public static HttpResponseMessage Ok(string body) =>
            new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _respond(request);
        }
    }
}
