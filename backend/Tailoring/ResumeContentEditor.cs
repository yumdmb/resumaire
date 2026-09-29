using System.Text.RegularExpressions;
using Resumaire.Api.Contracts;

namespace Resumaire.Api.Tailoring;

public static class TailoringOperations
{
    /// <summary>Replace the text at a leaf path (summary, headline, one bullet or detail line).</summary>
    public const string Replace = "Replace";

    /// <summary>Append a new bullet or detail line to a list path such as Experience[0].Bullets.</summary>
    public const string AddBullet = "AddBullet";

    /// <summary>Reorder or trim the flat skills list. Only skills already on the resume are allowed.</summary>
    public const string SetSkills = "SetSkills";

    public static readonly IReadOnlyList<string> All = [Replace, AddBullet, SetSkills];
}

/// <summary>
/// Single source of truth for where a tailoring suggestion may point and how it is applied.
/// Guardrails use it to validate model output, and the save endpoint uses it to build the tailored
/// resume on the server, so a suggestion can never overwrite identity fields like roles or employers.
/// </summary>
public static partial class ResumeContentEditor
{
    public static readonly IReadOnlyList<string> Sections =
        ["PersonalInfo", "Summary", "Skills", "Experience", "Education", "Projects", "Activities", "Certifications", "Links"];

    /// <summary>Skill categories are part of the Skills section, so their paths report "Skills".</summary>
    public static string SectionOf(string path)
    {
        var end = path.IndexOfAny(['[', '.']);
        var section = end < 0 ? path : path[..end];
        return section == "SkillGroups" ? "Skills" : section;
    }

    /// <summary>Reads the text at any known resume path, for evidence and original-content lookups.</summary>
    public static bool TryReadText(ResumeContentDto content, string path, out string text)
    {
        text = string.Empty;

        switch (path)
        {
            case "Summary":
                text = content.Summary ?? string.Empty;
                return !string.IsNullOrWhiteSpace(text);
            case "PersonalInfo.Headline":
                text = content.PersonalInfo?.Headline ?? string.Empty;
                return !string.IsNullOrWhiteSpace(text);
        }

        var skillMatch = SkillPathRegex().Match(path);
        if (skillMatch.Success)
        {
            return TryAt(content.Skills, Index(skillMatch, "i"), out text);
        }

        var groupMatch = SkillGroupPathRegex().Match(path);
        if (groupMatch.Success && TryAt(content.SkillGroups, Index(groupMatch, "i"), out var group))
        {
            if (!groupMatch.Groups["j"].Success)
            {
                text = JoinGroup(group);
                return !string.IsNullOrWhiteSpace(text);
            }

            return TryAt(group.Items, Index(groupMatch, "j"), out text);
        }

        var match = EntryPathRegex().Match(path);
        if (!match.Success)
        {
            return false;
        }

        var index = Index(match, "i");
        var field = match.Groups["f"].Value;
        var nested = match.Groups["j"].Success ? Index(match, "j") : -1;

        switch (match.Groups["sec"].Value)
        {
            case "Experience" when TryAt(content.Experience, index, out var experience):
                return field switch
                {
                    "Role" when nested < 0 => NonEmpty(experience.Role, out text),
                    "Organization" when nested < 0 => NonEmpty(experience.Organization, out text),
                    "Bullets" when nested >= 0 => TryAt(experience.Bullets, nested, out text),
                    _ => false
                };
            case "Education" when TryAt(content.Education, index, out var education):
                return field switch
                {
                    "Institution" when nested < 0 => NonEmpty(education.Institution, out text),
                    "Degree" when nested < 0 => NonEmpty(education.Degree, out text),
                    "Field" when nested < 0 => NonEmpty(education.Field, out text),
                    "Details" when nested >= 0 => TryAt(education.Details, nested, out text),
                    _ => false
                };
            case "Projects" when TryAt(content.Projects, index, out var project):
                return field switch
                {
                    "Name" when nested < 0 => NonEmpty(project.Name, out text),
                    "Technologies" when nested < 0 => NonEmpty(project.Technologies, out text),
                    "Bullets" when nested >= 0 => TryAt(project.Bullets, nested, out text),
                    _ => false
                };
            case "Activities" when TryAt(content.Activities, index, out var activity):
                return field switch
                {
                    "Title" when nested < 0 => NonEmpty(activity.Title, out text),
                    "Role" when nested < 0 => NonEmpty(activity.Role, out text),
                    "Bullets" when nested >= 0 => TryAt(activity.Bullets, nested, out text),
                    _ => false
                };
            case "Certifications" when TryAt(content.Certifications, index, out var certification):
                return field switch
                {
                    "Name" when nested < 0 => NonEmpty(certification.Name, out text),
                    "Issuer" when nested < 0 => NonEmpty(certification.Issuer, out text),
                    _ => false
                };
            case "Links" when TryAt(content.Links, index, out var link):
                return field == "Label" && nested < 0 && NonEmpty(link.Label, out text);
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks that a target path is editable and consistent with the operation.
    /// For <see cref="TailoringOperations.Replace"/> the current text at the path is returned as <paramref name="original"/>.
    /// </summary>
    public static bool TryValidateTarget(
        ResumeContentDto content,
        string path,
        string operation,
        out string? original,
        out string error)
    {
        original = null;
        error = string.Empty;

        switch (operation)
        {
            case TailoringOperations.SetSkills:
                // With categories the flat list is only a mirror, so edits must go through a category.
                var hasGroups = content.SkillGroups is { Count: > 0 };
                if (path == "Skills" && !hasGroups)
                {
                    original = string.Join(", ", content.Skills ?? []);
                    return true;
                }

                var setGroup = SkillGroupOnlyRegex().Match(path);
                if (setGroup.Success && TryAt(content.SkillGroups, Index(setGroup, "i"), out var target))
                {
                    original = string.Join(", ", target.Items ?? []);
                    return true;
                }

                error = hasGroups
                    ? "SetSkills must target one skill category, such as 'SkillGroups[0]'."
                    : "SetSkills must target 'Skills'.";
                return false;

            case TailoringOperations.AddBullet:
                var listMatch = ListPathRegex().Match(path);
                if (!listMatch.Success || !TryGetList(content, listMatch, out _))
                {
                    error = $"AddBullet target '{path}' is not an existing bullet list.";
                    return false;
                }

                return true;

            case TailoringOperations.Replace:
                if (path is "Summary" or "PersonalInfo.Headline")
                {
                    original = TryReadText(content, path, out var current) ? current : null;
                    return true;
                }

                var leafMatch = LeafPathRegex().Match(path);
                if (!leafMatch.Success || !TryReadText(content, path, out var leafText))
                {
                    error = $"Replace target '{path}' is not an editable bullet, summary or headline.";
                    return false;
                }

                original = leafText;
                return true;

            default:
                error = $"Operation '{operation}' is not supported.";
                return false;
        }
    }

    /// <summary>Applies one validated suggestion to a resume, returning a new immutable content object.</summary>
    public static ResumeContentDto Apply(ResumeContentDto content, string path, string operation, string text)
    {
        var value = text.Trim();
        if (value.Length == 0)
        {
            return content;
        }

        switch (operation)
        {
            case TailoringOperations.SetSkills:
                // Reordering and trimming only: a skill that is not already in the target list is dropped.
                var groupTarget = SkillGroupOnlyRegex().Match(path);
                if (groupTarget.Success)
                {
                    var groupIndex = Index(groupTarget, "i");
                    if (!TryAt(content.SkillGroups, groupIndex, out var group))
                    {
                        return content;
                    }

                    var groupItems = ReorderSubset(group.Items, value);
                    if (groupItems.Length == 0)
                    {
                        return content;
                    }

                    var groups = Replace(content.SkillGroups!, groupIndex, g => g with { Items = groupItems });
                    var mirrored = groups
                        .SelectMany(g => g.Items ?? [])
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    return content with { SkillGroups = groups, Skills = mirrored };
                }

                var skills = ReorderSubset(content.Skills, value);
                return skills.Length == 0 ? content : content with { Skills = skills };

            case TailoringOperations.Replace when path == "Summary":
                return content with { Summary = value };

            case TailoringOperations.Replace when path == "PersonalInfo.Headline":
                return content.PersonalInfo is null
                    ? content
                    : content with { PersonalInfo = content.PersonalInfo with { Headline = value } };

            case TailoringOperations.Replace:
                var leaf = LeafPathRegex().Match(path);
                return leaf.Success ? ChangeList(content, leaf, list => Replace(list, Index(leaf, "j"), value)) : content;

            case TailoringOperations.AddBullet:
                var listMatch = ListPathRegex().Match(path);
                return listMatch.Success ? ChangeList(content, listMatch, list => [.. list, value]) : content;

            default:
                return content;
        }
    }

    private static ResumeContentDto ChangeList(
        ResumeContentDto content,
        Match match,
        Func<IReadOnlyList<string>, IReadOnlyList<string>> change)
    {
        var index = Index(match, "i");
        if (!TryGetList(content, match, out var list))
        {
            return content;
        }

        var updated = change(list);

        switch (match.Groups["sec"].Value)
        {
            case "Experience":
                return content with { Experience = Replace(content.Experience!, index, e => e with { Bullets = updated }) };
            case "Education":
                return content with { Education = Replace(content.Education!, index, e => e with { Details = updated }) };
            case "Projects":
                return content with { Projects = Replace(content.Projects!, index, e => e with { Bullets = updated }) };
            case "Activities":
                return content with { Activities = Replace(content.Activities!, index, e => e with { Bullets = updated }) };
            default:
                return content;
        }
    }

    private static bool TryGetList(ResumeContentDto content, Match match, out IReadOnlyList<string> list)
    {
        list = [];
        var index = Index(match, "i");
        var field = match.Groups["f"].Value;

        switch (match.Groups["sec"].Value)
        {
            case "Experience" when field == "Bullets" && TryAt(content.Experience, index, out var experience):
                list = experience.Bullets ?? [];
                return true;
            case "Education" when field == "Details" && TryAt(content.Education, index, out var education):
                list = education.Details ?? [];
                return true;
            case "Projects" when field == "Bullets" && TryAt(content.Projects, index, out var project):
                list = project.Bullets ?? [];
                return true;
            case "Activities" when field == "Bullets" && TryAt(content.Activities, index, out var activity):
                list = activity.Bullets ?? [];
                return true;
            default:
                return false;
        }
    }

    private static string[] ReorderSubset(IReadOnlyList<string>? existingItems, string text)
    {
        var existing = (existingItems ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return text
            .Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(existing.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string JoinGroup(ResumeSkillGroupDto group) => string.Join(", ", group.Items ?? []);

    private static IReadOnlyList<string> Replace(IReadOnlyList<string> list, int index, string value)
    {
        if (index < 0 || index >= list.Count)
        {
            return list;
        }

        var copy = list.ToArray();
        copy[index] = value;
        return copy;
    }

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> list, int index, Func<T, T> change)
    {
        var copy = list.ToArray();
        copy[index] = change(copy[index]);
        return copy;
    }

    private static bool TryAt<T>(IReadOnlyList<T>? list, int index, out T value)
    {
        if (list is not null && index >= 0 && index < list.Count && list[index] is { } item)
        {
            value = item;
            return true;
        }

        value = default!;
        return false;
    }

    private static bool NonEmpty(string? value, out string text)
    {
        text = value ?? string.Empty;
        return !string.IsNullOrWhiteSpace(text);
    }

    private static int Index(Match match, string group) =>
        int.TryParse(match.Groups[group].Value, out var index) ? index : -1;

    [GeneratedRegex(@"^Skills\[(?<i>\d+)\]$")]
    private static partial Regex SkillPathRegex();

    [GeneratedRegex(@"^SkillGroups\[(?<i>\d+)\](?:\.Items\[(?<j>\d+)\])?$")]
    private static partial Regex SkillGroupPathRegex();

    [GeneratedRegex(@"^SkillGroups\[(?<i>\d+)\]$")]
    private static partial Regex SkillGroupOnlyRegex();

    [GeneratedRegex(@"^(?<sec>Experience|Education|Projects|Activities|Certifications|Links)\[(?<i>\d+)\](?:\.(?<f>[A-Za-z]+)(?:\[(?<j>\d+)\])?)?$")]
    private static partial Regex EntryPathRegex();

    [GeneratedRegex(@"^(?<sec>Experience|Education|Projects|Activities)\[(?<i>\d+)\]\.(?<f>Bullets|Details)$")]
    private static partial Regex ListPathRegex();

    [GeneratedRegex(@"^(?<sec>Experience|Education|Projects|Activities)\[(?<i>\d+)\]\.(?<f>Bullets|Details)\[(?<j>\d+)\]$")]
    private static partial Regex LeafPathRegex();
}
