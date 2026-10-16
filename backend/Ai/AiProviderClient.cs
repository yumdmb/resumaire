using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Resumaire.Api.Ai;

public sealed record AiModelCatalog(IReadOnlyList<AiModel> Models, bool IsFallback, string? Notice);
public interface IAiProviderClient
{
    Task<AiModelCatalog> DiscoverAsync(string userId, AiProvider provider, string key, string version, bool refresh, CancellationToken cancellationToken);
    Task<string> RespondAsync(AiConnection connection, object body, CancellationToken cancellationToken);
}

public sealed class AiProviderClient(HttpClient client, AiProviderRegistry registry, IMemoryCache cache) : IAiProviderClient
{
    public async Task<AiModelCatalog> DiscoverAsync(string userId, AiProvider provider, string key, string version, bool refresh, CancellationToken cancellationToken)
    {
        var cacheKey = $"ai-models:{userId}:{provider.Id}:{version}";
        var cacheable = !version.StartsWith("draft:", StringComparison.Ordinal);
        if (cacheable && !refresh && cache.TryGetValue<AiModelCatalog>(cacheKey, out var cached)) return cached!;
        using var request = Request(provider, key, HttpMethod.Get, "models");
        using var response = await SendAsync(request, cancellationToken);
        AiModelCatalog catalog;
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
        {
            catalog = new(provider.Models, true, "Model discovery is unavailable or needs additional permissions. These supported models are suggestions; test your selected model to verify access.");
        }
        else
        {
            EnsureSuccess(response.StatusCode);
            try
            {
                using var document = JsonDocument.Parse(await ReadBodyAsync(response, cancellationToken));
                var data = document.RootElement.GetProperty("data");
                if (data.ValueKind != JsonValueKind.Array) throw new JsonException();
                var models = data.EnumerateArray().Take(2000)
                    .Where(item => item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetProperty("id").GetString()!)
                    .Where(id => id.Length is > 0 and <= 200 && !id.Any(char.IsControl))
                    .Distinct(StringComparer.Ordinal).Select(id => registry.Describe(provider.Id, id))
                    .OrderByDescending(model => model.Supported).ThenByDescending(model => model.Recommended).ThenBy(model => model.Name)
                    .ToArray();
                catalog = new(models, false, models.Any(model => model.Supported) ? null : "The provider listed no models currently supported for resume tailoring.");
            }
            catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
            {
                throw new AiConnectionException(502, "invalid_catalog", "The provider returned an unreadable model catalog. Try again later.");
            }
        }
        // The cache contains model metadata only; a credential version prevents reuse after replacement/deletion.
        if (cacheable) cache.Set(cacheKey, catalog, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), Size = 4096 + catalog.Models.Count * 512 });
        return catalog;
    }

    public async Task<string> RespondAsync(AiConnection connection, object body, CancellationToken cancellationToken)
    {
        using var request = Request(connection.Provider, connection.ApiKey, HttpMethod.Post, "responses");
        request.Content = JsonContent.Create(body);
        using var response = await SendAsync(request, cancellationToken);
        EnsureSuccess(response.StatusCode);
        return await ReadBodyAsync(response, cancellationToken);
    }

    private static HttpRequestMessage Request(AiProvider provider, string key, HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(provider.BaseUrl), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try { return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken); }
        catch (HttpRequestException) { throw new AiConnectionException(502, "provider_unreachable", "The AI provider could not be reached. Try again later."); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new AiConnectionException(504, "provider_timeout", "The AI provider timed out. Try again later."); }
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > 2 * 1024 * 1024)
            throw new AiConnectionException(502, "provider_response_too_large", "The provider response was too large.");
        try
        {
            await response.Content.LoadIntoBufferAsync(2 * 1024 * 1024, cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException) { throw new AiConnectionException(502, "provider_response_unreadable", "The provider response could not be read."); }
    }

    private static void EnsureSuccess(HttpStatusCode status)
    {
        if ((int)status is >= 200 and < 300) return;
        throw (int)status switch
        {
            401 => new AiConnectionException(409, "invalid_api_key", "The provider rejected your API key. Update it in AI settings."),
            403 => new AiConnectionException(409, "provider_permission", "Your key does not have permission to use this model. Check provider permissions or choose another model."),
            429 => new AiConnectionException(429, "provider_quota", "Your provider's quota or rate limit was reached. Check your provider balance and limits.", 60),
            400 or 404 => new AiConnectionException(422, "model_unavailable", "The selected model or request format is unavailable. Choose and test another model in AI settings."),
            >= 500 => new AiConnectionException(502, "provider_unavailable", "The AI provider is unavailable. Try again later."),
            _ => new AiConnectionException(502, "provider_error", "The AI provider request failed.")
        };
    }
}
