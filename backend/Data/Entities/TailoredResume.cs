namespace Resumaire.Api.Data.Entities;

public sealed class TailoredResume
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    public Guid SourceBaseResumeId { get; set; }

    public BaseResume SourceBaseResume { get; set; } = null!;

    public int SourceBaseResumeRevision { get; set; }

    public string SourceBaseResumeContentJson { get; set; } = "{}";

    public int SchemaVersion { get; set; } = 1;

    public int VersionNumber { get; set; } = 1;

    public string? Name { get; set; }

    public string ContentJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<TailoringSuggestion> TailoringSuggestions { get; } = [];
}
