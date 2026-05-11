using Microsoft.AspNetCore.Identity;

namespace Resumaire.Api.Data;

public sealed class ApplicationUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
