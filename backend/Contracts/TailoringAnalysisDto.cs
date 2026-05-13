using Resumaire.Api.Tailoring;

namespace Resumaire.Api.Contracts;

public sealed record TailoringAnalysisResponse(
    Guid JobId,
    Guid BaseResumeId,
    int BaseResumeRevision,
    IReadOnlyList<JobKeywordDto> ExtractedKeywords,
    ResumeKeywordComparisonDto Comparison);

public sealed record JobKeywordDto(
    string Text,
    string Category,
    IReadOnlyList<string> Aliases,
    int MentionCount);

public sealed record ResumeKeywordEvidenceDto(
    string Section,
    string Path,
    string Text);

public sealed record SupportedResumeKeywordDto(
    JobKeywordDto Keyword,
    IReadOnlyList<ResumeKeywordEvidenceDto> Evidence);

public sealed record MissingResumeKeywordDto(JobKeywordDto Keyword);

public sealed record ResumeKeywordReorderOpportunityDto(
    JobKeywordDto Keyword,
    IReadOnlyList<ResumeKeywordEvidenceDto> CurrentEvidence,
    IReadOnlyList<string> SuggestedSections);

public sealed record ResumeKeywordComparisonDto(
    IReadOnlyList<SupportedResumeKeywordDto> SupportedKeywords,
    IReadOnlyList<MissingResumeKeywordDto> MissingKeywords,
    IReadOnlyList<ResumeKeywordReorderOpportunityDto> ReorderOpportunities);

public static class TailoringAnalysisDtoMapper
{
    public static TailoringAnalysisResponse ToResponse(
        Guid jobId,
        Guid baseResumeId,
        int baseResumeRevision,
        IReadOnlyList<JobKeyword> extractedKeywords,
        ResumeKeywordComparisonResult comparison) =>
        new(
            jobId,
            baseResumeId,
            baseResumeRevision,
            extractedKeywords.Select(ToDto).ToArray(),
            ToDto(comparison));

    private static ResumeKeywordComparisonDto ToDto(ResumeKeywordComparisonResult comparison) =>
        new(
            comparison.SupportedKeywords.Select(ToDto).ToArray(),
            comparison.MissingKeywords.Select(ToDto).ToArray(),
            comparison.ReorderOpportunities.Select(ToDto).ToArray());

    private static SupportedResumeKeywordDto ToDto(SupportedResumeKeyword keyword) =>
        new(
            ToDto(keyword.Keyword),
            keyword.Evidence.Select(ToDto).ToArray());

    private static MissingResumeKeywordDto ToDto(MissingResumeKeyword keyword) =>
        new(ToDto(keyword.Keyword));

    private static ResumeKeywordReorderOpportunityDto ToDto(ResumeKeywordReorderOpportunity opportunity) =>
        new(
            ToDto(opportunity.Keyword),
            opportunity.CurrentEvidence.Select(ToDto).ToArray(),
            opportunity.SuggestedSections);

    private static JobKeywordDto ToDto(JobKeyword keyword) =>
        new(
            keyword.Text,
            keyword.Category,
            keyword.Aliases,
            keyword.MentionCount);

    private static ResumeKeywordEvidenceDto ToDto(ResumeKeywordEvidence evidence) =>
        new(
            evidence.Section,
            evidence.Path,
            evidence.Text);
}
