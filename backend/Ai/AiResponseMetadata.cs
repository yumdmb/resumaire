using System.Text.Json;

namespace Resumaire.Api.Ai;

public static class AiResponseMetadata
{
    public static (int? Input, int? Output) Tokens(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object) return (null, null);
        return (Token(usage, "input_tokens"), Token(usage, "output_tokens"));
    }
    private static int? Token(JsonElement usage, string name) => usage.TryGetProperty(name, out var token)
        && token.ValueKind == JsonValueKind.Number && token.TryGetInt32(out var count) && count >= 0 ? count : null;

    public static string? OutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var text) && text.ValueKind == JsonValueKind.String) return text.GetString();
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in output.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var part in content.EnumerateArray())
                if (part.ValueKind == JsonValueKind.Object && part.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "output_text"
                    && part.TryGetProperty("text", out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        }
        return null;
    }
}
