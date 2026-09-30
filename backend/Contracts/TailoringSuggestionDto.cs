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
    string TargetPath,
    string Operation,
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

/// <summary>
/// When accepted suggestions carry a target path, the server builds the tailored content itself from the
/// current base resume plus those suggestions (using <see cref="SuggestionEdits"/> where the user edited the text).
/// <see cref="Content"/> is then ignored. It is used for manual tailoring and for suggestions saved before targets existed.
/// </summary>
public sealed record SaveTailoredResumeRequest(
    string? Name,
    ResumeContentDto? Content,
    IReadOnlyCollection<Guid>? AcceptedSuggestionIds,
    IReadOnlyCollection<Guid>? RejectedSuggestionIds,
    IReadOnlyDictionary<Guid, string>? SuggestionEdits = null);

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
