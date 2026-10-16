using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;

namespace Resumaire.Api.Ai;

public sealed class AiConnectionRequest
{
    public string? ProviderId { get; init; }
    public string? ModelId { get; init; }
    public string? ApiKey { get; init; }
    public bool Refresh { get; init; }
    public bool TrainingAcknowledged { get; init; }
    public string? ValidationToken { get; init; }
    public Guid? ExpectedVersion { get; init; }
}
public sealed record AiSettingsResponse(bool HasKey, string? ProviderId, string? ModelId, string? KeyLastFour,
    Guid? CredentialVersion, DateTimeOffset? LastValidatedAt, bool TrainingAcknowledged,
    IReadOnlyList<AiProviderSummary> Providers);
public sealed record AiProviderSummary(string Id, string Name, string PrivacyUrl);
public sealed record AiTestResponse(string ValidationToken, DateTimeOffset TestedAt, string Message);

public sealed class AiSettingsService(ApplicationDbContext db, AiProviderRegistry registry, AiCredentialProtector protector,
    IAiProviderClient client, AiUsageService usage, TimeProvider clock)
{
    public async Task<AiSettingsResponse> GetAsync(string userId, CancellationToken cancellationToken) =>
        Map(await db.UserAiSettings.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken));

    public async Task<AiConnection> ResolveSavedAsync(string userId, CancellationToken cancellationToken)
    {
        var saved = await db.UserAiSettings.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        if (saved is null) throw new AiConnectionException(409, "ai_settings_required", "Connect an AI provider in Settings → AI before generating suggestions. You can still edit manually.");
        var model = registry.GetModel(saved.ProviderId, saved.ModelId);
        CheckTraining(model, saved.TrainingAcknowledged);
        return new(registry.Get(saved.ProviderId), model, protector.Unprotect(userId, saved.EncryptedApiKey));
    }

    public async Task<AiModelCatalog> DiscoverAsync(string userId, AiConnectionRequest request, CancellationToken cancellationToken)
    {
        var provider = registry.Get(request.ProviderId);
        var (key, version) = await ResolveKeyAsync(userId, provider.Id, request.ApiKey, cancellationToken);
        return await client.DiscoverAsync(userId, provider, key, version, request.Refresh, cancellationToken);
    }

    public async Task<AiTestResponse> TestAsync(string userId, AiConnectionRequest request, CancellationToken cancellationToken)
    {
        var connection = await ResolveDraftAsync(userId, request, cancellationToken);
        CheckTraining(connection.Model, request.TrainingAcknowledged);
        var record = await usage.ReserveAsync(userId, connection, "Test", cancellationToken);
        var status = "Failed";
        int? inputTokens = null, outputTokens = null;
        try
        {
            var body = new
            {
                model = connection.Model.Id,
                store = false,
                instructions = "Return a JSON object with ok set to true. This is a connection test using synthetic data.",
                input = "Check structured output compatibility.",
                max_output_tokens = 4000,
                text = new { format = new { type = "json_schema", name = "connection_test", strict = true,
                    schema = new { type = "object", properties = new { ok = new { type = "boolean" } }, required = new[] { "ok" }, additionalProperties = false } } }
            };
            using var document = JsonDocument.Parse(await client.RespondAsync(connection, body, cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new JsonException();
            (inputTokens, outputTokens) = AiResponseMetadata.Tokens(root);
            if (root.TryGetProperty("status", out var state) && state.ValueKind == JsonValueKind.String && state.GetString() == "incomplete") throw new JsonException();
            var output = AiResponseMetadata.OutputText(root);
            using var result = JsonDocument.Parse(output ?? "null");
            if (result.RootElement.ValueKind != JsonValueKind.Object || !result.RootElement.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                throw new JsonException();
            status = "Succeeded";
            return new(protector.IssueReceipt(userId, connection), clock.GetUtcNow(), "Connection and structured output verified. Save within ten minutes.");
        }
        catch (JsonException) { throw new AiConnectionException(422, "model_incompatible", "This model did not return the required structured output. Try another supported model."); }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        finally { await usage.FinishAsync(record, status, inputTokens, outputTokens); }
    }

    public async Task<AiSettingsResponse> SaveAsync(string userId, AiConnectionRequest request, CancellationToken cancellationToken)
    {
        var connection = await ResolveDraftAsync(userId, request, cancellationToken);
        CheckTraining(connection.Model, request.TrainingAcknowledged);
        var testedAt = protector.VerifyReceipt(userId, connection, request.ValidationToken);
        var saved = await db.UserAiSettings.SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        if (saved?.CredentialVersion != request.ExpectedVersion)
            throw new AiConnectionException(409, "settings_changed", "Your AI settings changed in another window. Reload settings before saving.");
        var now = clock.GetUtcNow();
        if (saved is null)
        {
            saved = new UserAiSettings { UserId = userId, ProviderId = connection.Provider.Id, ModelId = connection.Model.Id,
                EncryptedApiKey = "", KeyLastFour = "", CreatedAt = now };
            db.UserAiSettings.Add(saved);
        }
        saved.ProviderId = connection.Provider.Id;
        saved.ModelId = connection.Model.Id;
        saved.EncryptedApiKey = protector.Protect(userId, connection.ApiKey);
        saved.KeyLastFour = connection.ApiKey[^4..];
        saved.CredentialVersion = Guid.NewGuid();
        saved.TrainingAcknowledged = request.TrainingAcknowledged;
        saved.LastValidatedAt = testedAt;
        saved.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return Map(saved);
    }

    public Task<int> RemoveAsync(string userId, CancellationToken cancellationToken) =>
        db.UserAiSettings.Where(value => value.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    private async Task<AiConnection> ResolveDraftAsync(string userId, AiConnectionRequest request, CancellationToken cancellationToken)
    {
        var provider = registry.Get(request.ProviderId);
        var model = registry.GetModel(provider.Id, request.ModelId);
        var (key, _) = await ResolveKeyAsync(userId, provider.Id, request.ApiKey, cancellationToken);
        return new(provider, model, key);
    }

    private async Task<(string Key, string Version)> ResolveKeyAsync(string userId, string providerId, string? suppliedKey, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(suppliedKey))
        {
            var key = suppliedKey.Trim();
            if (key.Length is < 8 or > 4096 || key.Any(character => character <= 32 || character > 126))
                throw new AiConnectionException(400, "invalid_key_format", "Enter a valid provider API key without spaces or line breaks.");
            return (key, "draft:" + AiCredentialProtector.Fingerprint(key));
        }
        var saved = await db.UserAiSettings.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);
        if (saved is null || saved.ProviderId != providerId)
            throw new AiConnectionException(400, "api_key_required", "Enter an API key for the selected provider.");
        return (protector.Unprotect(userId, saved.EncryptedApiKey), saved.CredentialVersion.ToString());
    }

    private AiSettingsResponse Map(UserAiSettings? saved) => new(saved is not null, saved?.ProviderId, saved?.ModelId,
        saved?.KeyLastFour, saved?.CredentialVersion, saved?.LastValidatedAt, saved?.TrainingAcknowledged ?? false,
        registry.Providers.Select(provider => new AiProviderSummary(provider.Id, provider.Name, provider.PrivacyUrl)).ToArray());

    private static void CheckTraining(AiModel model, bool acknowledged)
    {
        if (model.UsesTrainingData && !acknowledged)
            throw new AiConnectionException(400, "training_acknowledgement_required", "This model may use your resume text for training. Acknowledge this in AI settings or choose another model.");
    }
}
