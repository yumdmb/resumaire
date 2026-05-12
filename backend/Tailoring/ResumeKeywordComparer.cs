using Resumaire.Api.Contracts;

namespace Resumaire.Api.Tailoring;

public sealed class ResumeKeywordComparer : IResumeKeywordComparer
{
    public ResumeKeywordComparisonResult Compare(
        IReadOnlyList<JobKeyword> jobKeywords,
        ResumeContentDto resumeContent)
    {
        if (jobKeywords.Count == 0)
        {
            return new ResumeKeywordComparisonResult([], [], []);
        }

        var resumeIndex = BuildResumeIndex(resumeContent);
        var supportedKeywords = new List<SupportedResumeKeyword>();
        var missingKeywords = new List<MissingResumeKeyword>();
        var reorderOpportunities = new List<ResumeKeywordReorderOpportunity>();

        foreach (var keyword in jobKeywords)
        {
            var aliases = keyword.Aliases.Count == 0
                ? [keyword.Text]
                : keyword.Aliases.Prepend(keyword.Text).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            var evidence = resumeIndex
                .Where(source => aliases.Any(alias =>
                    KeywordTextMatcher.CountTermMatches(source.NormalizedText, KeywordTextMatcher.Normalize(alias)) > 0))
                .Select(source => new ResumeKeywordEvidence(source.Section, source.Path, source.Text))
                .DistinctBy(source => new { source.Path, source.Text })
                .Take(MaxEvidenceItems)
                .ToArray();

            if (evidence.Length == 0)
            {
                missingKeywords.Add(new MissingResumeKeyword(keyword));
                continue;
            }

            supportedKeywords.Add(new SupportedResumeKeyword(keyword, evidence));

            var hasProminentEvidence = resumeIndex.Any(source =>
                source.IsProminent &&
                aliases.Any(alias =>
                    KeywordTextMatcher.CountTermMatches(source.NormalizedText, KeywordTextMatcher.Normalize(alias)) > 0));

            if (!hasProminentEvidence && keyword.Category is not KeywordCategories.Seniority)
            {
                reorderOpportunities.Add(new ResumeKeywordReorderOpportunity(
                    keyword,
                    evidence,
                    SuggestedProminentSections));
            }
        }

        return new ResumeKeywordComparisonResult(
            supportedKeywords,
            missingKeywords,
            reorderOpportunities);
    }

    private static IReadOnlyList<ResumeTextSource> BuildResumeIndex(ResumeContentDto resumeContent)
    {
        var sources = new List<ResumeTextSource>();

        AddIfPresent(sources, "PersonalInfo", "PersonalInfo.Headline", resumeContent.PersonalInfo?.Headline, isProminent: true);
        AddIfPresent(sources, "Summary", "Summary", resumeContent.Summary, isProminent: true);

        if (resumeContent.Skills is not null)
        {
            for (var index = 0; index < resumeContent.Skills.Count; index++)
            {
                AddIfPresent(sources, "Skills", $"Skills[{index}]", resumeContent.Skills[index], isProminent: true);
            }
        }

        if (resumeContent.Experience is not null)
        {
            for (var index = 0; index < resumeContent.Experience.Count; index++)
            {
                var experience = resumeContent.Experience[index];
                AddIfPresent(sources, "Experience", $"Experience[{index}].Role", experience.Role, isProminent: false);
                AddIfPresent(sources, "Experience", $"Experience[{index}].Organization", experience.Organization, isProminent: false);

                if (experience.Bullets is null)
                {
                    continue;
                }

                for (var bulletIndex = 0; bulletIndex < experience.Bullets.Count; bulletIndex++)
                {
                    AddIfPresent(
                        sources,
                        "Experience",
                        $"Experience[{index}].Bullets[{bulletIndex}]",
                        experience.Bullets[bulletIndex],
                        isProminent: false);
                }
            }
        }

        if (resumeContent.Education is not null)
        {
            for (var index = 0; index < resumeContent.Education.Count; index++)
            {
                var education = resumeContent.Education[index];
                AddIfPresent(sources, "Education", $"Education[{index}].Institution", education.Institution, isProminent: false);
                AddIfPresent(sources, "Education", $"Education[{index}].Degree", education.Degree, isProminent: false);
                AddIfPresent(sources, "Education", $"Education[{index}].Field", education.Field, isProminent: false);

                if (education.Details is null)
                {
                    continue;
                }

                for (var detailIndex = 0; detailIndex < education.Details.Count; detailIndex++)
                {
                    AddIfPresent(
                        sources,
                        "Education",
                        $"Education[{index}].Details[{detailIndex}]",
                        education.Details[detailIndex],
                        isProminent: false);
                }
            }
        }

        if (resumeContent.Certifications is not null)
        {
            for (var index = 0; index < resumeContent.Certifications.Count; index++)
            {
                var certification = resumeContent.Certifications[index];
                AddIfPresent(sources, "Certifications", $"Certifications[{index}].Name", certification.Name, isProminent: false);
                AddIfPresent(sources, "Certifications", $"Certifications[{index}].Issuer", certification.Issuer, isProminent: false);
            }
        }

        if (resumeContent.Links is not null)
        {
            for (var index = 0; index < resumeContent.Links.Count; index++)
            {
                AddIfPresent(sources, "Links", $"Links[{index}].Label", resumeContent.Links[index].Label, isProminent: false);
            }
        }

        return sources;
    }

    private static void AddIfPresent(
        List<ResumeTextSource> sources,
        string section,
        string path,
        string? value,
        bool isProminent)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmedValue = value.Trim();
        sources.Add(new ResumeTextSource(
            section,
            path,
            trimmedValue.Length > MaxEvidenceTextLength
                ? string.Concat(trimmedValue.AsSpan(0, MaxEvidenceTextLength), "...")
                : trimmedValue,
            KeywordTextMatcher.Normalize(trimmedValue),
            isProminent));
    }

    private static readonly string[] SuggestedProminentSections = [ "Summary", "Skills" ];

    private const int MaxEvidenceItems = 3;
    private const int MaxEvidenceTextLength = 240;

    private sealed record ResumeTextSource(
        string Section,
        string Path,
        string Text,
        string NormalizedText,
        bool IsProminent);
}
