using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Resumaire.Api.Ai;

public sealed class AiCredentialProtector(IDataProtectionProvider provider, TimeProvider clock)
{
    private IDataProtector ForUser(string userId) => provider.CreateProtector("Resumaire.Ai.Credential.v1", userId);
    public string Protect(string userId, string key) => ForUser(userId).Protect(key);
    public string Unprotect(string userId, string ciphertext)
    {
        try { return ForUser(userId).Unprotect(ciphertext); }
        catch (CryptographicException)
        {
            throw new AiConnectionException(409, "credential_unreadable", "Your saved API key cannot be read. Replace it in AI settings.");
        }
    }

    public string IssueReceipt(string userId, AiConnection connection) =>
        provider.CreateProtector("Resumaire.Ai.Validation.v1", userId).Protect(JsonSerializer.Serialize(
            new Receipt(connection.Provider.Id, connection.Model.Id, Fingerprint(connection.ApiKey), clock.GetUtcNow().ToUnixTimeSeconds())));

    public DateTimeOffset VerifyReceipt(string userId, AiConnection connection, string? token)
    {
        try
        {
            if (string.IsNullOrEmpty(token) || token.Length > 4096) throw new CryptographicException();
            var receipt = JsonSerializer.Deserialize<Receipt>(provider.CreateProtector("Resumaire.Ai.Validation.v1", userId).Unprotect(token));
            var now = clock.GetUtcNow().ToUnixTimeSeconds();
            if (receipt is null || receipt.Provider != connection.Provider.Id || receipt.Model != connection.Model.Id
                || receipt.KeyHash != Fingerprint(connection.ApiKey) || receipt.TestedAt > now || now - receipt.TestedAt > 600)
                throw new CryptographicException();
            return DateTimeOffset.FromUnixTimeSeconds(receipt.TestedAt);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            throw new AiConnectionException(400, "test_required", "Test this provider, key and model before saving. Tests expire after ten minutes.");
        }
    }

    public static string Fingerprint(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    private sealed record Receipt(string Provider, string Model, string KeyHash, long TestedAt);
}
