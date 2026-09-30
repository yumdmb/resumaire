namespace Resumaire.Api.Data.Entities;

public sealed class TailoringSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public Guid? TailoredResumeId { get; set; }

    public TailoredResume? TailoredResume { get; set; }

    public Guid? SourceBaseResumeId { get; set; }

    public BaseResume? SourceBaseResume { get; set; }

    public TailoringSuggestionReviewState ReviewState { get; set; } = TailoringSuggestionReviewState.Pending;

    public string TargetSection { get; set; } = string.Empty;

    /// <summary>Editable resume location this suggestion changes, for example Experience[0].Bullets[1].</summary>
    public string TargetPath { get; set; } = string.Empty;

    /// <summary>Replace, AddBullet or SetSkills. See <c>TailoringOperations</c>.</summary>
    public string Operation { get; set; } = "Replace";

    /// <summary>Base resume revision the suggestion was generated from. Null for rows created before revisions were tracked.</summary>
    public int? SourceBaseResumeRevision { get; set; }

    public string? OriginalContentJson { get; set; }

    public string SuggestedContentJson { get; set; } = "{}";

    public string? AcceptedContentJson { get; set; }

    public string? Rationale { get; set; }

    public string? AiNotes { get; set; }

    public string? SourceEvidenceJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }
}
