namespace Resumaire.Api.Data.Entities;

public sealed class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public string Company { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Link { get; set; }

    public string Description { get; set; } = string.Empty;

    public JobStatus Status { get; set; } = JobStatus.Saved;

    public DateOnly? DateApplied { get; set; }

    public string? Notes { get; set; }

    public Guid? SelectedBaseResumeId { get; set; }

    public BaseResume? SelectedBaseResume { get; set; }

    public Guid? SelectedTailoredResumeId { get; set; }

    public TailoredResume? SelectedTailoredResume { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<TailoredResume> TailoredResumes { get; } = [];

    public ICollection<TailoringSuggestion> TailoringSuggestions { get; } = [];
}
