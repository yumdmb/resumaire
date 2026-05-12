namespace Resumaire.Api.Contracts;

public sealed record TailoringSuggestionBatchResponse(
    Guid JobId,
    Guid BaseResumeId,
    int BaseResumeRevision,
    IReadOnlyCollection<TailoringSuggestionResponse> Suggestions,
    IReadOnlyCollection<TailoringGapNoteResponse> GapNotes,
    IReadOnlyCollection<string> GuardrailRejections);

public sealed record TailoringSuggestionResponse(
    Guid Id,
    string ReviewState,
    string TargetSection,
    string? OriginalContent,
    string SuggestedContent,
    string? Rationale,
    string? AiNotes,
    IReadOnlyCollection<TailoringSourceEvidenceResponse> SourceEvidence,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt);

public sealed record TailoringSourceEvidenceResponse(
    string Section,
    string Path,
    string Text);

public sealed record TailoringGapNoteResponse(
    string Keyword,
    string Reason);

public sealed record SaveTailoredResumeRequest(
    string? Name,
    ResumeContentDto? Content,
    IReadOnlyCollection<Guid>? AcceptedSuggestionIds,
    IReadOnlyCollection<Guid>? RejectedSuggestionIds);

public sealed record TailoredResumeDetailResponse(
    Guid Id,
    int VersionNumber,
    string? Name,
    Guid JobId,
    Guid SourceBaseResumeId,
    int SourceBaseResumeRevision,
    int SchemaVersion,
    ResumeContentDto Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
