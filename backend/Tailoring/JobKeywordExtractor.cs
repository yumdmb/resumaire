using System.Text.RegularExpressions;

namespace Resumaire.Api.Tailoring;

public sealed class JobKeywordExtractor : IJobKeywordExtractor
{
    private static readonly Regex SplitListRegex = new(@"\s*(?:,|/|\||\band\b|\bor\b)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RequirementPhraseRegex = new(
        @"(?:experience with|knowledge of|proficiency in|proficient with|familiarity with|required skills include|skills include|using|with)\s+([^.;:\n\r]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyList<KnownKeyword> KnownKeywords =
    [
        new("ASP.NET Core", KeywordCategories.Framework, [ "asp.net core", "aspnet core" ]),
        new("ASP.NET", KeywordCategories.Framework, [ "asp.net", "aspnet" ]),
        new(".NET", KeywordCategories.Framework, [ ".net", "dotnet", "dot net" ]),
        new("Entity Framework Core", KeywordCategories.Framework, [ "entity framework core", "ef core" ]),
        new("React", KeywordCategories.Framework, [ "react", "react.js", "reactjs" ]),
        new("Next.js", KeywordCategories.Framework, [ "next.js", "nextjs" ]),
        new("Angular", KeywordCategories.Framework, [ "angular" ]),
        new("Vue", KeywordCategories.Framework, [ "vue", "vue.js", "vuejs" ]),
        new("Node.js", KeywordCategories.Framework, [ "node.js", "nodejs", "node" ]),
        new("TypeScript", KeywordCategories.Skill, [ "typescript" ]),
        new("JavaScript", KeywordCategories.Skill, [ "javascript" ]),
        new("C#", KeywordCategories.Skill, [ "c#" ]),
        new("SQL", KeywordCategories.Skill, [ "sql" ]),
        new("Python", KeywordCategories.Skill, [ "python" ]),
        new("Java", KeywordCategories.Skill, [ "java" ]),
        new("Go", KeywordCategories.Skill, [ "go", "golang" ]),
        new("REST APIs", KeywordCategories.Skill, [ "rest api", "rest apis", "restful api", "restful apis", "api", "apis" ]),
        new("GraphQL", KeywordCategories.Skill, [ "graphql" ]),
        new("Microservices", KeywordCategories.Skill, [ "microservices", "microservice" ]),
        new("PostgreSQL", KeywordCategories.Tool, [ "postgresql", "postgres" ]),
        new("SQL Server", KeywordCategories.Tool, [ "sql server", "mssql" ]),
        new("Redis", KeywordCategories.Tool, [ "redis" ]),
        new("Docker", KeywordCategories.Tool, [ "docker", "containerization", "containers" ]),
        new("Kubernetes", KeywordCategories.Tool, [ "kubernetes", "k8s" ]),
        new("Azure", KeywordCategories.Tool, [ "azure", "microsoft azure" ]),
        new("AWS", KeywordCategories.Tool, [ "aws", "amazon web services" ]),
        new("GCP", KeywordCategories.Tool, [ "gcp", "google cloud" ]),
        new("Kafka", KeywordCategories.Tool, [ "kafka", "apache kafka" ]),
        new("RabbitMQ", KeywordCategories.Tool, [ "rabbitmq" ]),
        new("Git", KeywordCategories.Tool, [ "git", "github", "gitlab" ]),
        new("CI/CD", KeywordCategories.Tool, [ "ci/cd", "cicd", "continuous integration", "continuous delivery", "continuous deployment" ]),
        new("Testing", KeywordCategories.Responsibility, [ "testing", "unit tests", "integration tests", "test automation" ]),
        new("Mentoring", KeywordCategories.Responsibility, [ "mentor", "mentoring", "coach", "coaching" ]),
        new("Leadership", KeywordCategories.Responsibility, [ "lead", "leading", "leadership" ]),
        new("Architecture", KeywordCategories.Responsibility, [ "architecture", "architect", "system design", "technical design" ]),
        new("Performance optimization", KeywordCategories.Responsibility, [ "performance", "optimization", "optimise", "optimize", "scalability", "scalable" ]),
        new("Observability", KeywordCategories.Responsibility, [ "observability", "monitoring", "logging", "metrics", "tracing" ]),
        new("Senior", KeywordCategories.Seniority, [ "senior", "sr." ]),
        new("Staff", KeywordCategories.Seniority, [ "staff" ]),
        new("Principal", KeywordCategories.Seniority, [ "principal" ]),
        new("Lead", KeywordCategories.Seniority, [ "lead" ]),
        new("Junior", KeywordCategories.Seniority, [ "junior", "entry level", "entry-level" ])
    ];

    public JobKeywordExtractionResult Extract(string jobDescription)
    {
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            return new JobKeywordExtractionResult([]);
        }

        var normalizedText = KeywordTextMatcher.Normalize(jobDescription);
        var keywords = new List<KeywordCandidate>();

        foreach (var knownKeyword in KnownKeywords)
        {
            var mentionCount = knownKeyword.Aliases.Sum(alias =>
                KeywordTextMatcher.CountTermMatches(normalizedText, KeywordTextMatcher.Normalize(alias)));
            if (mentionCount > 0)
            {
                keywords.Add(new KeywordCandidate(
                    knownKeyword.Text,
                    knownKeyword.Category,
                    knownKeyword.Aliases,
                    mentionCount,
                    FindFirstIndex(normalizedText, knownKeyword.Aliases)));
            }
        }

        foreach (var inferredKeyword in ExtractInferredKeywords(jobDescription))
        {
            if (keywords.Any(keyword => IsSameKeyword(keyword.Text, inferredKeyword)))
            {
                continue;
            }

            keywords.Add(new KeywordCandidate(
                inferredKeyword,
                KeywordCategories.Skill,
                [inferredKeyword],
                KeywordTextMatcher.CountTermMatches(normalizedText, KeywordTextMatcher.Normalize(inferredKeyword)),
                normalizedText.IndexOf(KeywordTextMatcher.Normalize(inferredKeyword), StringComparison.Ordinal)));
        }

        var result = keywords
            .OrderByDescending(keyword => keyword.MentionCount)
            .ThenBy(keyword => keyword.FirstIndex < 0 ? int.MaxValue : keyword.FirstIndex)
            .ThenBy(keyword => keyword.Text, StringComparer.OrdinalIgnoreCase)
            .Select(keyword => new JobKeyword(
                keyword.Text,
                keyword.Category,
                keyword.Aliases.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                keyword.MentionCount))
            .ToArray();

        return new JobKeywordExtractionResult(result);
    }

    private static IEnumerable<string> ExtractInferredKeywords(string jobDescription)
    {
        foreach (Match match in RequirementPhraseRegex.Matches(jobDescription))
        {
            foreach (var part in SplitListRegex.Split(match.Groups[1].Value))
            {
                var keyword = CleanInferredKeyword(part);
                if (keyword is not null)
                {
                    yield return keyword;
                }
            }
        }
    }

    private static string? CleanInferredKeyword(string value)
    {
        var keyword = value.Trim().Trim('.', ';', ':', '-', ' ');

        if (keyword.Length < 2 || keyword.Length > 50)
        {
            return null;
        }

        var lower = keyword.ToLowerInvariant();
        if (IgnoredInferredKeywords.Contains(lower))
        {
            return null;
        }

        if (lower.StartsWith("the ", StringComparison.Ordinal) ||
            lower.StartsWith("a ", StringComparison.Ordinal) ||
            lower.StartsWith("an ", StringComparison.Ordinal))
        {
            return null;
        }

        return keyword;
    }

    private static int FindFirstIndex(string normalizedText, IReadOnlyList<string> aliases)
    {
        var indexes = aliases
            .Select(alias => normalizedText.IndexOf(KeywordTextMatcher.Normalize(alias), StringComparison.Ordinal))
            .Where(index => index >= 0)
            .ToArray();

        return indexes.Length == 0 ? -1 : indexes.Min();
    }

    private static bool IsSameKeyword(string left, string right) =>
        string.Equals(
            KeywordTextMatcher.Normalize(left),
            KeywordTextMatcher.Normalize(right),
            StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<string> IgnoredInferredKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "teams",
        "team",
        "systems",
        "software",
        "applications",
        "services",
        "products",
        "engineers",
        "stakeholders"
    };

    private sealed record KnownKeyword(string Text, string Category, IReadOnlyList<string> Aliases);

    private sealed record KeywordCandidate(
        string Text,
        string Category,
        IReadOnlyList<string> Aliases,
        int MentionCount,
        int FirstIndex);
}
