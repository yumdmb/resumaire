namespace Resumaire.Api.Tailoring;

public static class KeywordCategories
{
    public const string Skill = "Skill";
    public const string Tool = "Tool";
    public const string Framework = "Framework";
    public const string Responsibility = "Responsibility";
    public const string Seniority = "Seniority";
}

public sealed record JobKeyword(
    string Text,
    string Category,
    IReadOnlyList<string> Aliases,
    int MentionCount);

public sealed record JobKeywordExtractionResult(IReadOnlyList<JobKeyword> Keywords);

public sealed record ResumeKeywordEvidence(
    string Section,
    string Path,
    string Text);

public sealed record SupportedResumeKeyword(
    JobKeyword Keyword,
    IReadOnlyList<ResumeKeywordEvidence> Evidence);

public sealed record MissingResumeKeyword(JobKeyword Keyword);

public sealed record ResumeKeywordReorderOpportunity(
    JobKeyword Keyword,
    IReadOnlyList<ResumeKeywordEvidence> CurrentEvidence,
    IReadOnlyList<string> SuggestedSections);

public sealed record ResumeKeywordComparisonResult(
    IReadOnlyList<SupportedResumeKeyword> SupportedKeywords,
    IReadOnlyList<MissingResumeKeyword> MissingKeywords,
    IReadOnlyList<ResumeKeywordReorderOpportunity> ReorderOpportunities);
