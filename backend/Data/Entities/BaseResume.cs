namespace Resumaire.Api.Data.Entities;

public sealed class BaseResume
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int SchemaVersion { get; set; } = 1;

    public int Revision { get; set; } = 1;

    public string ContentJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<TailoredResume> TailoredResumes { get; } = [];

    public ICollection<TailoringSuggestion> TailoringSuggestions { get; } = [];
}
