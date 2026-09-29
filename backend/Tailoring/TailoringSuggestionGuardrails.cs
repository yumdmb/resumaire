using System.Text.RegularExpressions;
using Resumaire.Api.Contracts;

namespace Resumaire.Api.Tailoring;

public sealed partial class TailoringSuggestionGuardrails
{
    private const int MaxSuggestionLength = 1000;

    public TailoringSuggestionGuardrailResult Validate(
        AiTailoringSuggestionGenerationResult generationResult,
        ResumeContentDto resumeContent,
        ResumeKeywordComparisonResult comparison)
    {
        var resumeText = string.Join(
            "\n",
            FlattenResumeText(resumeContent).Where(value => !string.IsNullOrWhiteSpace(value)));
        var normalizedResumeText = KeywordTextMatcher.Normalize(resumeText);
        var metricSearchText = NormalizeMetricText(resumeText);

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
                resumeContent,
                normalizedResumeText,
                metricSearchText,
                evidenceByPath,
                missingKeywordTerms,
                out var sourceEvidence,
                out var originalContent);

            if (errors.Count > 0)
            {
                rejections.AddRange(errors.Select(error => $"{suggestion.TargetSection}: {error}"));
                continue;
            }

            accepted.Add(new ValidatedTailoringSuggestionDraft(
                suggestion.TargetSection,
                originalContent,
                suggestion.SuggestedContent.Trim(),
                suggestion.Rationale.Trim(),
                sourceEvidence,
                suggestion.AiNotes,
                suggestion.TargetPath,
                suggestion.Operation));
        }

        return new TailoringSuggestionGuardrailResult(accepted, rejections);
    }

    private static List<string> ValidateSuggestion(
        AiTailoringSuggestionDraft suggestion,
        ResumeContentDto resumeContent,
        string normalizedResumeText,
        string metricSearchText,
        IReadOnlyDictionary<string, ResumeKeywordEvidence> evidenceByPath,
        IReadOnlyList<string> missingKeywordTerms,
        out IReadOnlyList<ResumeKeywordEvidence> sourceEvidence,
        out string? originalContent)
    {
        var errors = new List<string>();
        sourceEvidence = [];
        originalContent = null;

        if (!ResumeContentEditor.Sections.Contains(suggestion.TargetSection, StringComparer.Ordinal))
        {
            errors.Add("Target section is unsupported.");
        }

        if (string.IsNullOrWhiteSpace(suggestion.SuggestedContent))
        {
            errors.Add("Suggested content is required.");
        }
        else if (suggestion.SuggestedContent.Length > MaxSuggestionLength)
        {
            errors.Add($"Suggested content cannot exceed {MaxSuggestionLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(suggestion.Rationale))
        {
            errors.Add("Rationale is required.");
        }

        if (suggestion.SourceEvidencePaths.Count == 0)
        {
            errors.Add("At least one source evidence path is required.");
        }

        // The target decides which resume field changes, so it must be an editable path that agrees with the section.
        var targetIsValid = ResumeContentEditor.TryValidateTarget(
            resumeContent,
            suggestion.TargetPath,
            suggestion.Operation,
            out originalContent,
            out var targetError);

        if (!targetIsValid)
        {
            errors.Add(targetError);
        }
        else if (!string.Equals(
                     ResumeContentEditor.SectionOf(suggestion.TargetPath),
                     suggestion.TargetSection,
                     StringComparison.Ordinal))
        {
            errors.Add($"Target path '{suggestion.TargetPath}' does not belong to section '{suggestion.TargetSection}'.");
        }

        var resolvedEvidence = new List<ResumeKeywordEvidence>();
        foreach (var path in suggestion.SourceEvidencePaths)
        {
            if (evidenceByPath.TryGetValue(path, out var evidence))
            {
                resolvedEvidence.Add(evidence);
                continue;
            }

            // Any readable resume path is valid evidence, not only keyword-linked ones, so short resumes still work.
            if (ResumeContentEditor.TryReadText(resumeContent, path, out var text))
            {
                resolvedEvidence.Add(new ResumeKeywordEvidence(ResumeContentEditor.SectionOf(path), path, text));
                continue;
            }

            errors.Add($"Source evidence path '{path}' is not supported by the base resume.");
        }

        sourceEvidence = resolvedEvidence
            .DistinctBy(evidence => new { evidence.Path, evidence.Text })
            .ToArray();

        if (string.IsNullOrWhiteSpace(suggestion.SuggestedContent))
        {
            return errors;
        }

        if (suggestion.Operation == TailoringOperations.SetSkills)
        {
            // originalContent is the current list at the target: the flat skills, or one category's items.
            ValidateSkillsSubset(suggestion.SuggestedContent, originalContent, errors);
        }
        else
        {
            // A rewrite is grounded in the text it replaces as well as in the cited evidence.
            var grounding = originalContent is { Length: > 0 }
                ? resolvedEvidence.Append(new ResumeKeywordEvidence(suggestion.TargetSection, suggestion.TargetPath, originalContent))
                : resolvedEvidence;

            if (!HasEvidenceOverlap(suggestion.SuggestedContent, grounding.ToArray()))
            {
                errors.Add("Suggested content is not grounded in the cited resume evidence.");
            }
        }

        var normalizedSuggestion = KeywordTextMatcher.Normalize(suggestion.SuggestedContent);
        foreach (var missingTerm in missingKeywordTerms)
        {
            if (KeywordTextMatcher.CountTermMatches(normalizedSuggestion, missingTerm) > 0 &&
                KeywordTextMatcher.CountTermMatches(normalizedResumeText, missingTerm) == 0)
            {
                errors.Add($"Suggested content adds unsupported keyword '{missingTerm}'.");
            }
        }

        foreach (Match match in MetricClaimRegex().Matches(suggestion.SuggestedContent))
        {
            if (!ContainsMetric(metricSearchText, match.Value))
            {
                errors.Add($"Suggested content adds unsupported metric '{match.Value}'.");
            }
        }

        foreach (Match match in ProtectedPhraseRegex().Matches(suggestion.SuggestedContent))
        {
            var phrase = StripSentenceStartWord(suggestion.SuggestedContent, match);
            if (phrase is null || IgnoredProtectedPhrases.Contains(phrase))
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

    private static void ValidateSkillsSubset(
        string suggestedContent,
        string? currentSkills,
        List<string> errors)
    {
        var existing = (currentSkills ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suggested = suggestedContent
            .Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (suggested.Length == 0)
        {
            errors.Add("Suggested skills list is empty.");
        }

        foreach (var skill in suggested.Where(skill => !existing.Contains(skill)))
        {
            errors.Add($"Suggested skills add '{skill}', which is not on the resume.");
        }
    }

    private static string? StripSentenceStartWord(string text, Match match)
    {
        var phrase = match.Value.Trim();
        var before = text[..match.Index].TrimEnd();
        var atSentenceStart = before.Length == 0 || before[^1] is '.' or '!' or '?' or ':' or '\n' or '•' or '-';

        if (!atSentenceStart)
        {
            return phrase;
        }

        // "Built React dashboards": the first capital is grammar, not a named claim.
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 2 ? null : string.Join(' ', words.Skip(1));
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
            .Select(match => match.Value.TrimEnd('.'))
            .Where(term => term.Length >= 3 || ShortTechTerms.Contains(term))
            .Where(term => !StopWords.Contains(term))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static bool ContainsMetric(string metricSearchText, string metric)
    {
        var normalized = NormalizeMetricText(metric);
        if (normalized.Length == 0)
        {
            return true;
        }

        return Regex.IsMatch(
            metricSearchText,
            $@"(?<![\d.$]){Regex.Escape(normalized)}(?![\d])",
            RegexOptions.IgnoreCase);
    }

    /// <summary>Lower-cases, and removes thousands separators and spaces before '%' so "1,000" equals "1000" and "40 %" equals "40%".</summary>
    private static string NormalizeMetricText(string value)
    {
        var lowered = value.ToLowerInvariant();
        lowered = ThousandsSeparatorRegex().Replace(lowered, "$1$2");
        lowered = SpaceBeforePercentRegex().Replace(lowered, "%");
        return Regex.Replace(lowered, @"\s+", " ").Trim();
    }

    private static IEnumerable<string> FlattenResumeText(ResumeContentDto resumeContent)
    {
        yield return resumeContent.PersonalInfo?.FullName ?? string.Empty;
        yield return resumeContent.PersonalInfo?.Headline ?? string.Empty;
        yield return resumeContent.Summary ?? string.Empty;

        foreach (var skill in resumeContent.Skills ?? [])
        {
            yield return skill;
        }

        foreach (var group in resumeContent.SkillGroups ?? [])
        {
            yield return group.Category ?? string.Empty;

            foreach (var item in group.Items ?? [])
            {
                yield return item;
            }
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

        foreach (var project in resumeContent.Projects ?? [])
        {
            yield return project.Name ?? string.Empty;
            yield return project.Technologies ?? string.Empty;

            foreach (var bullet in project.Bullets ?? [])
            {
                yield return bullet;
            }
        }

        foreach (var activity in resumeContent.Activities ?? [])
        {
            yield return activity.Title ?? string.Empty;
            yield return activity.Role ?? string.Empty;

            foreach (var bullet in activity.Bullets ?? [])
            {
                yield return bullet;
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

    private static readonly HashSet<string> ShortTechTerms = new(StringComparer.Ordinal)
    {
        "go", "ci", "cd", "ai", "ml", "qa", "ui", "ux", "db", "c#", "r"
    };

    private static readonly HashSet<string> IgnoredProtectedPhrases = new(StringComparer.Ordinal)
    {
        "REST APIs"
    };

    [GeneratedRegex(@"\b\d+(?:\.\d+)?\s?%|\$\d+(?:,\d{3})*(?:\.\d+)?(?:\s?[kmb]\b)?|\b\d+(?:\.\d+)?x\b|\b\d+\+?\s+(?:users|customers|requests|teams|engineers|projects|years|months|revenue|cost|latency|uptime)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MetricClaimRegex();

    [GeneratedRegex(@"\b(?:[A-Z][A-Za-z0-9+#.]*(?:\s+[A-Z][A-Za-z0-9+#.]*){1,4})\b")]
    private static partial Regex ProtectedPhraseRegex();

    [GeneratedRegex(@"[a-z0-9+#]+(?:\.[a-z0-9+#]+)*\.?")]
    private static partial Regex SignificantTermRegex();

    [GeneratedRegex(@"(\d),(\d{3})")]
    private static partial Regex ThousandsSeparatorRegex();

    [GeneratedRegex(@"\s+%")]
    private static partial Regex SpaceBeforePercentRegex();
}
