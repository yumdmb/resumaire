using System.Text.Json;
using Resumaire.Api.Contracts;

namespace Resumaire.Api.Export;

/// <summary>Maps resume content to the flat data.json shape consumed by Typst/resume.typ.</summary>
public static class TypstResumeDataMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string ToDataJson(ResumeContentDto content) =>
        JsonSerializer.Serialize(Map(content), JsonOptions);

    public static TypstResumeData Map(ResumeContentDto content)
    {
        var info = content.PersonalInfo;

        var contact = new List<TypstContactItem>();
        AddContact(contact, info?.Phone, string.IsNullOrWhiteSpace(info?.Phone) ? "" : "tel:" + Compact(info!.Phone!));
        AddContact(contact, info?.Location, "");
        AddContact(contact, info?.Email, string.IsNullOrWhiteSpace(info?.Email) ? "" : "mailto:" + info!.Email!.Trim());
        AddContact(contact, DisplayUrl(info?.Website), Clean(info?.Website));

        foreach (var link in content.Links ?? [])
        {
            if (!string.IsNullOrWhiteSpace(link.Label))
            {
                AddContact(contact, link.Label, Clean(link.Url));
            }
        }

        var skills = new List<TypstSkillGroup>();
        if (content.SkillGroups is { Count: > 0 })
        {
            foreach (var group in content.SkillGroups)
            {
                var items = JoinItems(group.Items);
                if (items.Length > 0)
                {
                    skills.Add(new TypstSkillGroup(Clean(group.Category), items));
                }
            }
        }
        else
        {
            var items = JoinItems(content.Skills);
            if (items.Length > 0)
            {
                skills.Add(new TypstSkillGroup("", items));
            }
        }

        return new TypstResumeData(
            Name: Clean(info?.FullName, "Resume"),
            Contact: contact,
            Summary: Clean(content.Summary),
            Education: (content.Education ?? []).Select(e => new TypstEducation(
                Clean(e.Institution),
                Clean(e.Location),
                JoinNonEmpty(", ", e.Degree, e.Field),
                DateRange(e.StartDate, e.EndDate, isCurrent: false),
                Bullets(e.Details))).ToList(),
            Experience: (content.Experience ?? []).Select(e => new TypstExperience(
                Clean(e.Role),
                DateRange(e.StartDate, e.EndDate, e.IsCurrent),
                Clean(e.Organization),
                Clean(e.Location),
                Bullets(e.Bullets))).ToList(),
            Projects: (content.Projects ?? []).Select(p => new TypstProject(
                Clean(p.Name),
                Clean(p.Url),
                Clean(p.Technologies),
                Bullets(p.Bullets))).ToList(),
            Skills: skills,
            Certifications: (content.Certifications ?? []).Select(c => new TypstCertification(
                Clean(c.Name),
                Clean(c.IssuedDate),
                Clean(c.Issuer))).ToList(),
            Activities: (content.Activities ?? []).Select(a => new TypstActivity(
                Clean(a.Title),
                Clean(a.Location),
                Clean(a.Role),
                Clean(a.Date),
                Bullets(a.Bullets))).ToList());
    }

    private static void AddContact(List<TypstContactItem> items, string? text, string url)
    {
        var value = Clean(text);
        if (value.Length > 0)
        {
            items.Add(new TypstContactItem(value, url));
        }
    }

    private static string DisplayUrl(string? url)
    {
        var value = Clean(url);
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme is "http" or "https"))
        {
            return (uri.Host + uri.PathAndQuery).TrimEnd('/');
        }

        return value;
    }

    private static string DateRange(string? start, string? end, bool isCurrent)
    {
        var from = Clean(start);
        var to = isCurrent ? "Present" : Clean(end);

        if (from.Length > 0 && to.Length > 0)
        {
            return $"{from} -- {to}";
        }

        return from.Length > 0 ? from : to;
    }

    private static string Compact(string value) => new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private static string Clean(string? value, string fallback = "") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string JoinNonEmpty(string separator, params string?[] values) =>
        string.Join(separator, values.Select(v => Clean(v)).Where(v => v.Length > 0));

    private static string JoinItems(IReadOnlyList<string>? items) =>
        string.Join(", ", (items ?? []).Select(v => Clean(v)).Where(v => v.Length > 0));

    private static List<string> Bullets(IReadOnlyList<string>? values) =>
        (values ?? []).Select(v => Clean(v)).Where(v => v.Length > 0).ToList();
}

public sealed record TypstResumeData(
    string Name,
    IReadOnlyList<TypstContactItem> Contact,
    string Summary,
    IReadOnlyList<TypstEducation> Education,
    IReadOnlyList<TypstExperience> Experience,
    IReadOnlyList<TypstProject> Projects,
    IReadOnlyList<TypstSkillGroup> Skills,
    IReadOnlyList<TypstCertification> Certifications,
    IReadOnlyList<TypstActivity> Activities);

public sealed record TypstContactItem(string Text, string Url);

public sealed record TypstEducation(string Institution, string Location, string Degree, string Dates, IReadOnlyList<string> Bullets);

public sealed record TypstExperience(string Title, string Dates, string Organization, string Location, IReadOnlyList<string> Bullets);

public sealed record TypstProject(string Name, string Url, string Technologies, IReadOnlyList<string> Bullets);

public sealed record TypstSkillGroup(string Category, string Items);

public sealed record TypstCertification(string Name, string Date, string Issuer);

public sealed record TypstActivity(string Title, string Location, string Role, string Date, IReadOnlyList<string> Bullets);
