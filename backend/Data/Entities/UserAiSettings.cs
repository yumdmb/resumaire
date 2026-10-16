namespace Resumaire.Api.Data.Entities;

public sealed class UserAiSettings
{
    public required string UserId { get; set; }
    public required string ProviderId { get; set; }
    public required string ModelId { get; set; }
    public required string EncryptedApiKey { get; set; }
    public required string KeyLastFour { get; set; }
    public Guid CredentialVersion { get; set; } = Guid.NewGuid();
    public DateTimeOffset LastValidatedAt { get; set; }
    public bool TrainingAcknowledged { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
