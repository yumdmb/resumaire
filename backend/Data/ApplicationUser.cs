using Microsoft.AspNetCore.Identity;
using Resumaire.Api.Data.Entities;

namespace Resumaire.Api.Data;

public sealed class ApplicationUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Job> Jobs { get; } = [];

    public BaseResume? BaseResume { get; set; }

    public ICollection<TailoredResume> TailoredResumes { get; } = [];

    public ICollection<TailoringSuggestion> TailoringSuggestions { get; } = [];
}
