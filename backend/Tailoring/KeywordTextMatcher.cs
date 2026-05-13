using System.Text.RegularExpressions;

namespace Resumaire.Api.Tailoring;

internal static class KeywordTextMatcher
{
    public static int CountTermMatches(string normalizedText, string normalizedTerm)
    {
        if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(normalizedTerm))
        {
            return 0;
        }

        var pattern = $@"(?<![a-z0-9+#.]){Regex.Escape(normalizedTerm)}(?![a-z0-9+#.])";
        return Regex.Matches(normalizedText, pattern, RegexOptions.IgnoreCase).Count;
    }

    public static string Normalize(string value) =>
        Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();
}
