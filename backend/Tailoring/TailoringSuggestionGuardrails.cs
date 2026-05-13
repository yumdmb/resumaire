using System.Text.RegularExpressions;
using Resumaire.Api.Contracts;

namespace Resumaire.Api.Tailoring;

public sealed partial class TailoringSuggestionGuardrails
{
    private static readonly HashSet<string> ValidTargetSections = new(StringComparer.Ordinal)
    {
        "PersonalInfo",
        "Summary",
        "Skills",
        "Experience",
        "Education",
        "Certifications",
        "Links"
    };

    public TailoringSuggestionGuardrailResult Validate(
        AiTailoringSuggestionGenerationResult generationResult,
        ResumeContentDto resumeContent,
        ResumeKeywordComparisonResult comparison)
    {
        var resumeText = string.Join(
            "\n",
            FlattenResumeText(resumeContent).Where(value => !string.IsNullOrWhiteSpace(value)));
        var normalizedResumeText = KeywordTextMatcher.Normalize(resumeText);

        var evidenceByPath = BuildEvidenceByPath(comparison);
        var missingKeywordTerms = comparison.MissingKeywords
            .SelectMany(keyword => keyword.Keyword.Aliases.Prepend(keyword.Keyword.Text))
            .Select(KeywordTextMatcher.Normalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var accepted = new List<ValidatedTailoringSuggestionDraft>();
        var rejections = new List<string>();

        foreach (var suggestion in generationResult.Suggestions)
        {
            var errors = ValidateSuggestion(
                suggestion,
                normalizedResumeText,
                evidenceByPath,
                missingKeywordTerms,
                out var sourceEvidence);

            if (errors.Count > 0)
            {
                rejections.AddRange(errors.Select(error => $"{suggestion.TargetSection}: {error}"));
                continue;
            }

            accepted.Add(new ValidatedTailoringSuggestionDraft(
                suggestion.TargetSection,
                suggestion.OriginalContent,
                suggestion.SuggestedContent.Trim(),
                suggestion.Rationale.Trim(),
                sourceEvidence,
                suggestion.AiNotes));
        }

        return new TailoringSuggestionGuardrailResult(accepted, rejections);
    }

    private static IReadOnlyList<string> ValidateSuggestion(
        AiTailoringSuggestionDraft suggestion,
        string normalizedResumeText,
        IReadOnlyDictionary<string, ResumeKeywordEvidence> evidenceByPath,
        IReadOnlyList<string> missingKeywordTerms,
        out IReadOnlyList<ResumeKeywordEvidence> sourceEvidence)
    {
        var errors = new List<string>();
        sourceEvidence = [];

        if (!ValidTargetSections.Contains(suggestion.TargetSection))
        {
            errors.Add("Target section is unsupported.");
        }

        if (string.IsNullOrWhiteSpace(suggestion.SuggestedContent))
        {
            errors.Add("Suggested content is required.");
        }

        if (string.IsNullOrWhiteSpace(suggestion.Rationale))
        {
            errors.Add("Rationale is required.");
        }

        if (suggestion.SourceEvidencePaths.Count == 0)
        {
            errors.Add("At least one source evidence path is required.");
        }

        var resolvedEvidence = new List<ResumeKeywordEvidence>();
        foreach (var path in suggestion.SourceEvidencePaths)
        {
            if (evidenceByPath.TryGetValue(path, out var evidence))
            {
                resolvedEvidence.Add(evidence);
                continue;
            }

            errors.Add($"Source evidence path '{path}' is not supported by the base resume.");
        }

        sourceEvidence = resolvedEvidence
            .DistinctBy(evidence => new { evidence.Path, evidence.Text })
            .ToArray();

        if (!HasEvidenceOverlap(suggestion.SuggestedContent, resolvedEvidence))
        {
            errors.Add("Suggested content is not grounded in the cited resume evidence.");
        }

        var normalizedSuggestion = KeywordTextMatcher.Normalize(suggestion.SuggestedContent);
        foreach (var missingTerm in missingKeywordTerms)
        {
            if (KeywordTextMatcher.CountTermMatches(normalizedSuggestion, missingTerm) > 0)
            {
                errors.Add($"Suggested content adds unsupported keyword '{missingTerm}'.");
            }
        }

        foreach (Match match in MetricClaimRegex().Matches(suggestion.SuggestedContent))
        {
            if (!normalizedResumeText.Contains(KeywordTextMatcher.Normalize(match.Value), StringComparison.Ordinal))
            {
                errors.Add($"Suggested content adds unsupported metric '{match.Value}'.");
            }
        }

        foreach (Match match in ProtectedPhraseRegex().Matches(suggestion.SuggestedContent))
        {
            var phrase = match.Value.Trim();
            if (IgnoredProtectedPhrases.Contains(phrase))
            {
                continue;
            }

            if (!normalizedResumeText.Contains(KeywordTextMatcher.Normalize(phrase), StringComparison.Ordinal))
            {
                errors.Add($"Suggested content adds unsupported named claim '{phrase}'.");
            }
        }

        return errors;
    }

    private static IReadOnlyDictionary<string, ResumeKeywordEvidence> BuildEvidenceByPath(
        ResumeKeywordComparisonResult comparison)
    {
        var evidence = comparison.SupportedKeywords
            .SelectMany(keyword => keyword.Evidence)
            .Concat(comparison.ReorderOpportunities.SelectMany(opportunity => opportunity.CurrentEvidence))
            .DistinctBy(value => value.Path);

        return evidence.ToDictionary(value => value.Path, StringComparer.Ordinal);
    }

    private static bool HasEvidenceOverlap(
        string suggestedContent,
        IReadOnlyList<ResumeKeywordEvidence> sourceEvidence)
    {
        if (sourceEvidence.Count == 0 || string.IsNullOrWhiteSpace(suggestedContent))
        {
            return false;
        }

        var suggestedTerms = ExtractSignificantTerms(suggestedContent);
        if (suggestedTerms.Count == 0)
        {
            return false;
        }

        var evidenceTerms = sourceEvidence
            .SelectMany(evidence => ExtractSignificantTerms(evidence.Text))
            .ToHashSet(StringComparer.Ordinal);

        return suggestedTerms.Count(term => evidenceTerms.Contains(term)) >= Math.Min(2, suggestedTerms.Count);
    }

    private static IReadOnlyList<string> ExtractSignificantTerms(string value) =>
        SignificantTermRegex()
            .Matches(KeywordTextMatcher.Normalize(value))
            .Select(match => match.Value)
            .Where(term => !StopWords.Contains(term))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> FlattenResumeText(ResumeContentDto resumeContent)
    {
        yield return resumeContent.PersonalInfo?.FullName ?? string.Empty;
        yield return resumeContent.PersonalInfo?.Headline ?? string.Empty;
        yield return resumeContent.Summary ?? string.Empty;

        foreach (var skill in resumeContent.Skills ?? [])
        {
            yield return skill;
        }

        foreach (var experience in resumeContent.Experience ?? [])
        {
            yield return experience.Role ?? string.Empty;
            yield return experience.Organization ?? string.Empty;

            foreach (var bullet in experience.Bullets ?? [])
            {
                yield return bullet;
            }
        }

        foreach (var education in resumeContent.Education ?? [])
        {
            yield return education.Institution ?? string.Empty;
            yield return education.Degree ?? string.Empty;
            yield return education.Field ?? string.Empty;

            foreach (var detail in education.Details ?? [])
            {
                yield return detail;
            }
        }

        foreach (var certification in resumeContent.Certifications ?? [])
        {
            yield return certification.Name ?? string.Empty;
            yield return certification.Issuer ?? string.Empty;
            yield return certification.CredentialId ?? string.Empty;
        }

        foreach (var link in resumeContent.Links ?? [])
        {
            yield return link.Label ?? string.Empty;
            yield return link.Url ?? string.Empty;
        }
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "and",
        "api",
        "apis",
        "build",
        "built",
        "for",
        "the",
        "with"
    };

    private static readonly HashSet<string> IgnoredProtectedPhrases = new(StringComparer.Ordinal)
    {
        "REST APIs"
    };

    [GeneratedRegex(@"\b\d+(?:\.\d+)?%|\$\d+(?:,\d{3})*(?:\.\d+)?|\b\d+(?:\.\d+)?x\b|\b\d+\+?\s+(?:users|customers|requests|teams|engineers|projects|years|months|revenue|cost|latency|uptime)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MetricClaimRegex();

    [GeneratedRegex(@"\b(?:[A-Z][A-Za-z0-9+#.]*(?:\s+[A-Z][A-Za-z0-9+#.]*){1,4})\b")]
    private static partial Regex ProtectedPhraseRegex();

    [GeneratedRegex(@"\b[a-z0-9+#.]{3,}\b")]
    private static partial Regex SignificantTermRegex();
}
