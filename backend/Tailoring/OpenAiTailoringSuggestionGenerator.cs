using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Resumaire.Api.Configuration;

namespace Resumaire.Api.Tailoring;

public sealed class OpenAiTailoringSuggestionGenerator(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiTailoringSuggestionGenerator> logger)
    : IAiTailoringSuggestionGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiTailoringSuggestionGenerationResult> GenerateAsync(
        AiTailoringSuggestionGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var openAiOptions = options.Value;
        if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
        {
            throw new AiTailoringUnavailableException("OpenAI:ApiKey is not configured.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiOptions.ApiKey);
        httpRequest.Content = JsonContent.Create(new
        {
            model = string.IsNullOrWhiteSpace(openAiOptions.Model) ? "gpt-4o-mini" : openAiOptions.Model,
            instructions = Instructions,
            input = BuildInput(request),
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "resume_tailoring_suggestions",
                    strict = true,
                    schema = JsonSerializer.Deserialize<JsonElement>(OutputSchemaJson)
                }
            },
            temperature = 0.2,
            max_output_tokens = 2500
        }, options: JsonOptions);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "OpenAI tailoring request failed with status {StatusCode}: {Body}",
                response.StatusCode,
                body);
            throw new AiTailoringProviderException("AI suggestion generation failed.");
        }

        using var document = JsonDocument.Parse(body);
        var outputText = ExtractOutputText(document.RootElement);
        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new AiTailoringProviderException("AI suggestion generation returned no structured output.");
        }

        var aiResponse = JsonSerializer.Deserialize<OpenAiTailoringResponse>(outputText, JsonOptions);
        if (aiResponse is null)
        {
            throw new AiTailoringProviderException("AI suggestion generation returned invalid structured output.");
        }

        return new AiTailoringSuggestionGenerationResult(
            aiResponse.Suggestions
                .Select(suggestion => new AiTailoringSuggestionDraft(
                    suggestion.TargetSection,
                    NormalizeOptional(suggestion.OriginalContent),
                    suggestion.SuggestedContent.Trim(),
                    suggestion.Rationale.Trim(),
                    suggestion.SourceEvidencePaths
                        .Where(path => !string.IsNullOrWhiteSpace(path))
                        .Select(path => path.Trim())
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    NormalizeOptional(suggestion.AiNotes)))
                .ToArray(),
            aiResponse.GapNotes
                .Select(note => new AiTailoringGapNote(note.Keyword.Trim(), note.Reason.Trim()))
                .Where(note => !string.IsNullOrWhiteSpace(note.Keyword))
                .ToArray());
    }

    private static string BuildInput(AiTailoringSuggestionGenerationRequest request)
    {
        var supportedKeywords = request.Comparison.SupportedKeywords.Select(keyword => new
        {
            keyword = keyword.Keyword.Text,
            category = keyword.Keyword.Category,
            evidence = keyword.Evidence.Select(evidence => new
            {
                evidence.Path,
                evidence.Section,
                evidence.Text
            })
        });

        var missingKeywords = request.Comparison.MissingKeywords.Select(keyword => new
        {
            keyword = keyword.Keyword.Text,
            category = keyword.Keyword.Category
        });

        var reorderOpportunities = request.Comparison.ReorderOpportunities.Select(opportunity => new
        {
            keyword = opportunity.Keyword.Text,
            suggestedSections = opportunity.SuggestedSections,
            currentEvidence = opportunity.CurrentEvidence.Select(evidence => new
            {
                evidence.Path,
                evidence.Section,
                evidence.Text
            })
        });

        return JsonSerializer.Serialize(new
        {
            job = new
            {
                request.Company,
                title = request.JobTitle,
                description = request.JobDescription
            },
            resume = request.ResumeContent,
            extractedKeywords = request.ExtractedKeywords,
            supportedKeywords,
            missingKeywords,
            reorderOpportunities
        }, JsonOptions);
    }

    private static string? ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output) ||
            output.ValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) ||
                content.ValueKind is not JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (!contentItem.TryGetProperty("type", out var type) ||
                    type.ValueKind is not JsonValueKind.String)
                {
                    continue;
                }

                if (type.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }

                if (type.GetString() == "refusal" &&
                    contentItem.TryGetProperty("refusal", out var refusal) &&
                    refusal.ValueKind == JsonValueKind.String)
                {
                    throw new AiTailoringProviderException("AI suggestion generation was refused.");
                }
            }
        }

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private const string Instructions = """
        You generate honest resume tailoring suggestions.
        Return JSON only in the requested schema.
        You may rephrase, reorder, or emphasize only content already present in the resume evidence.
        Every suggestion must cite one or more sourceEvidencePaths from supportedKeywords or reorderOpportunities.
        Do not invent companies, roles, projects, credentials, dates, metrics, responsibilities, tools, or experience.
        If a job keyword is missing from the resume evidence, add it to gapNotes and do not add it as experience.
        Prefer concise, section-level suggestions that a user can review before accepting.
        Use empty strings for originalContent or aiNotes when there is no useful value.
        """;

    private const string OutputSchemaJson = """
        {
          "type": "object",
          "properties": {
            "suggestions": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "targetSection": {
                    "type": "string",
                    "enum": ["PersonalInfo", "Summary", "Skills", "Experience", "Education", "Certifications", "Links"]
                  },
                  "originalContent": { "type": "string" },
                  "suggestedContent": { "type": "string" },
                  "rationale": { "type": "string" },
                  "sourceEvidencePaths": {
                    "type": "array",
                    "items": { "type": "string" }
                  },
                  "aiNotes": { "type": "string" }
                },
                "required": ["targetSection", "originalContent", "suggestedContent", "rationale", "sourceEvidencePaths", "aiNotes"],
                "additionalProperties": false
              }
            },
            "gapNotes": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "keyword": { "type": "string" },
                  "reason": { "type": "string" }
                },
                "required": ["keyword", "reason"],
                "additionalProperties": false
              }
            }
          },
          "required": ["suggestions", "gapNotes"],
          "additionalProperties": false
        }
        """;

    private sealed record OpenAiTailoringResponse(
        IReadOnlyList<OpenAiTailoringSuggestion> Suggestions,
        IReadOnlyList<OpenAiTailoringGapNote> GapNotes);

    private sealed record OpenAiTailoringSuggestion(
        string TargetSection,
        string OriginalContent,
        string SuggestedContent,
        string Rationale,
        IReadOnlyList<string> SourceEvidencePaths,
        string AiNotes);

    private sealed record OpenAiTailoringGapNote(string Keyword, string Reason);
}
