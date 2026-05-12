using Resumaire.Api.Contracts;

namespace Resumaire.Api.Tailoring;

public sealed record AiTailoringSuggestionGenerationRequest(
    string JobTitle,
    string Company,
    string JobDescription,
    ResumeContentDto ResumeContent,
    IReadOnlyList<JobKeyword> ExtractedKeywords,
    ResumeKeywordComparisonResult Comparison);

public sealed record AiTailoringSuggestionGenerationResult(
    IReadOnlyList<AiTailoringSuggestionDraft> Suggestions,
    IReadOnlyList<AiTailoringGapNote> GapNotes);

public sealed record AiTailoringSuggestionDraft(
    string TargetSection,
    string? OriginalContent,
    string SuggestedContent,
    string Rationale,
    IReadOnlyList<string> SourceEvidencePaths,
    string? AiNotes);

public sealed record AiTailoringGapNote(string Keyword, string Reason);

public sealed record ValidatedTailoringSuggestionDraft(
    string TargetSection,
    string? OriginalContent,
    string SuggestedContent,
    string Rationale,
    IReadOnlyList<ResumeKeywordEvidence> SourceEvidence,
    string? AiNotes);

public sealed record TailoringSuggestionGuardrailResult(
    IReadOnlyList<ValidatedTailoringSuggestionDraft> Suggestions,
    IReadOnlyList<string> Rejections);

public sealed class AiTailoringUnavailableException(string message) : InvalidOperationException(message);

public sealed class AiTailoringProviderException(string message) : InvalidOperationException(message);
