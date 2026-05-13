using System.Text;
using System.Web;
using Resumaire.Api.Contracts;

namespace Resumaire.Api.Export;

public sealed class ResumeHtmlRenderer : IResumeHtmlRenderer
{
    public string RenderToHtml(ResumeContentDto content)
    {
        var sb = new StringBuilder(4096);

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>");
        sb.Append(Encode(content.PersonalInfo?.FullName ?? "Resume"));
        sb.AppendLine("</title>");
        sb.AppendLine(StyleBlock);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div class=\"resume\">");

        RenderHeader(sb, content.PersonalInfo);
        RenderSummary(sb, content.Summary);
        RenderSkills(sb, content.Skills);
        RenderExperience(sb, content.Experience);
        RenderEducation(sb, content.Education);
        RenderCertifications(sb, content.Certifications);
        RenderLinks(sb, content.Links);

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void RenderHeader(StringBuilder sb, ResumePersonalInfoDto? info)
    {
        if (info is null) return;

        sb.AppendLine("<header class=\"header\">");

        if (!string.IsNullOrWhiteSpace(info.FullName))
        {
            sb.Append("<h1 class=\"name\">");
            sb.Append(Encode(info.FullName));
            sb.AppendLine("</h1>");
        }

        if (!string.IsNullOrWhiteSpace(info.Headline))
        {
            sb.Append("<p class=\"headline\">");
            sb.Append(Encode(info.Headline));
            sb.AppendLine("</p>");
        }

        var contactParts = new List<string>(4);
        if (!string.IsNullOrWhiteSpace(info.Email))
            contactParts.Add(Encode(info.Email));
        if (!string.IsNullOrWhiteSpace(info.Phone))
            contactParts.Add(Encode(info.Phone));
        if (!string.IsNullOrWhiteSpace(info.Location))
            contactParts.Add(Encode(info.Location));
        if (!string.IsNullOrWhiteSpace(info.Website))
            contactParts.Add($"<a href=\"{Encode(info.Website)}\">{Encode(info.Website)}</a>");

        if (contactParts.Count > 0)
        {
            sb.Append("<p class=\"contact\">");
            sb.Append(string.Join(" · ", contactParts));
            sb.AppendLine("</p>");
        }

        sb.AppendLine("</header>");
    }

    private static void RenderSummary(StringBuilder sb, string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return;

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2 class=\"section-title\">Summary</h2>");
        sb.Append("<p class=\"summary-text\">");
        sb.Append(Encode(summary));
        sb.AppendLine("</p>");
        sb.AppendLine("</section>");
    }

    private static void RenderSkills(StringBuilder sb, IReadOnlyList<string>? skills)
    {
        if (skills is null || skills.Count == 0) return;

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2 class=\"section-title\">Skills</h2>");
        sb.Append("<p class=\"skills-text\">");
        sb.Append(string.Join(", ", skills.Select(Encode)));
        sb.AppendLine("</p>");
        sb.AppendLine("</section>");
    }

    private static void RenderExperience(StringBuilder sb, IReadOnlyList<ResumeExperienceDto>? experience)
    {
        if (experience is null || experience.Count == 0) return;

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2 class=\"section-title\">Experience</h2>");

        foreach (var item in experience)
        {
            sb.AppendLine("<div class=\"entry\">");

            sb.AppendLine("<div class=\"entry-header\">");
            sb.Append("<div class=\"entry-primary\">");
            sb.Append("<span class=\"entry-title\">");
            sb.Append(Encode(item.Role ?? ""));
            sb.Append("</span>");
            if (!string.IsNullOrWhiteSpace(item.Organization))
            {
                sb.Append(" <span class=\"entry-org\">· ");
                sb.Append(Encode(item.Organization));
                sb.Append("</span>");
            }
            sb.AppendLine("</div>");

            var dateParts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(item.StartDate))
                dateParts.Add(Encode(item.StartDate));
            if (item.IsCurrent)
                dateParts.Add("Present");
            else if (!string.IsNullOrWhiteSpace(item.EndDate))
                dateParts.Add(Encode(item.EndDate));
            if (!string.IsNullOrWhiteSpace(item.Location))
                dateParts.Add(Encode(item.Location));

            if (dateParts.Count > 0)
            {
                sb.Append("<span class=\"entry-meta\">");
                sb.Append(string.Join(" · ", dateParts));
                sb.AppendLine("</span>");
            }

            sb.AppendLine("</div>");

            if (item.Bullets is { Count: > 0 })
            {
                sb.AppendLine("<ul class=\"bullets\">");
                foreach (var bullet in item.Bullets)
                {
                    sb.Append("<li>");
                    sb.Append(Encode(bullet));
                    sb.AppendLine("</li>");
                }
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</section>");
    }

    private static void RenderEducation(StringBuilder sb, IReadOnlyList<ResumeEducationDto>? education)
    {
        if (education is null || education.Count == 0) return;

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2 class=\"section-title\">Education</h2>");

        foreach (var item in education)
        {
            sb.AppendLine("<div class=\"entry\">");
            sb.AppendLine("<div class=\"entry-header\">");

            sb.Append("<div class=\"entry-primary\">");
            var titleParts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(item.Degree))
                titleParts.Add(Encode(item.Degree));
            if (!string.IsNullOrWhiteSpace(item.Field))
                titleParts.Add(Encode(item.Field));
            sb.Append("<span class=\"entry-title\">");
            sb.Append(string.Join(", ", titleParts));
            sb.Append("</span>");
            if (!string.IsNullOrWhiteSpace(item.Institution))
            {
                sb.Append(" <span class=\"entry-org\">· ");
                sb.Append(Encode(item.Institution));
                sb.Append("</span>");
            }
            sb.AppendLine("</div>");

            var metaParts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(item.StartDate))
                metaParts.Add(Encode(item.StartDate));
            if (!string.IsNullOrWhiteSpace(item.EndDate))
                metaParts.Add(Encode(item.EndDate));
            if (!string.IsNullOrWhiteSpace(item.Location))
                metaParts.Add(Encode(item.Location));

            if (metaParts.Count > 0)
            {
                sb.Append("<span class=\"entry-meta\">");
                sb.Append(string.Join(" · ", metaParts));
                sb.AppendLine("</span>");
            }

            sb.AppendLine("</div>");

            if (item.Details is { Count: > 0 })
            {
                sb.AppendLine("<ul class=\"bullets\">");
                foreach (var detail in item.Details)
                {
                    sb.Append("<li>");
                    sb.Append(Encode(detail));
                    sb.AppendLine("</li>");
                }
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</section>");
    }

    private static void RenderCertifications(StringBuilder sb, IReadOnlyList<ResumeCertificationDto>? certifications)
    {
        if (certifications is null || certifications.Count == 0) return;

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2 class=\"section-title\">Certifications</h2>");

        foreach (var item in certifications)
        {
            sb.AppendLine("<div class=\"entry\">");
            sb.AppendLine("<div class=\"entry-header\">");

            sb.Append("<div class=\"entry-primary\">");
            sb.Append("<span class=\"entry-title\">");
            sb.Append(Encode(item.Name ?? ""));
            sb.Append("</span>");
            if (!string.IsNullOrWhiteSpace(item.Issuer))
            {
                sb.Append(" <span class=\"entry-org\">· ");
                sb.Append(Encode(item.Issuer));
                sb.Append("</span>");
            }
            sb.AppendLine("</div>");

            var metaParts = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(item.IssuedDate))
                metaParts.Add(Encode(item.IssuedDate));
            if (!string.IsNullOrWhiteSpace(item.ExpirationDate))
                metaParts.Add($"Expires {Encode(item.ExpirationDate)}");

            if (metaParts.Count > 0)
            {
                sb.Append("<span class=\"entry-meta\">");
                sb.Append(string.Join(" · ", metaParts));
                sb.AppendLine("</span>");
            }

            sb.AppendLine("</div>");

            if (!string.IsNullOrWhiteSpace(item.CredentialId) || !string.IsNullOrWhiteSpace(item.Url))
            {
                sb.Append("<p class=\"cert-detail\">");
                if (!string.IsNullOrWhiteSpace(item.CredentialId))
                {
                    sb.Append("ID: ");
                    sb.Append(Encode(item.CredentialId));
                }
                if (!string.IsNullOrWhiteSpace(item.Url))
                {
                    if (!string.IsNullOrWhiteSpace(item.CredentialId))
                        sb.Append(" · ");
                    sb.Append($"<a href=\"{Encode(item.Url)}\">{Encode(item.Url)}</a>");
                }
                sb.AppendLine("</p>");
            }

            sb.AppendLine("</div>");
        }

        sb.AppendLine("</section>");
    }

    private static void RenderLinks(StringBuilder sb, IReadOnlyList<ResumeLinkDto>? links)
    {
        if (links is null || links.Count == 0) return;

        sb.AppendLine("<section class=\"section\">");
        sb.AppendLine("<h2 class=\"section-title\">Links</h2>");
        sb.AppendLine("<ul class=\"links-list\">");

        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Url)) continue;
            sb.Append("<li><a href=\"");
            sb.Append(Encode(link.Url));
            sb.Append("\">");
            sb.Append(Encode(link.Label ?? link.Url));
            sb.AppendLine("</a></li>");
        }

        sb.AppendLine("</ul>");
        sb.AppendLine("</section>");
    }

    private static string Encode(string? value) =>
        HttpUtility.HtmlEncode(value ?? "");

    private const string StyleBlock = """
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    font-size: 10pt;
    line-height: 1.5;
    color: #1a1a1a;
    background: #fff;
  }
  .resume {
    max-width: 8.5in;
    margin: 0 auto;
    padding: 0.6in 0.7in;
  }
  .header {
    margin-bottom: 18pt;
    padding-bottom: 12pt;
    border-bottom: 1.5pt solid #2a2a2a;
  }
  .name {
    font-size: 20pt;
    font-weight: 700;
    letter-spacing: -0.02em;
    line-height: 1.2;
    color: #111;
  }
  .headline {
    font-size: 11pt;
    color: #444;
    margin-top: 3pt;
  }
  .contact {
    font-size: 9pt;
    color: #555;
    margin-top: 6pt;
  }
  .contact a {
    color: #333;
    text-decoration: none;
  }
  .section {
    margin-bottom: 16pt;
  }
  .section-title {
    font-size: 11pt;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: #222;
    margin-bottom: 8pt;
    padding-bottom: 3pt;
    border-bottom: 0.75pt solid #ccc;
  }
  .summary-text {
    font-size: 10pt;
    color: #333;
    line-height: 1.6;
  }
  .skills-text {
    font-size: 10pt;
    color: #333;
    line-height: 1.6;
  }
  .entry {
    margin-bottom: 10pt;
  }
  .entry:last-child {
    margin-bottom: 0;
  }
  .entry-header {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    flex-wrap: wrap;
    gap: 4pt;
    margin-bottom: 3pt;
  }
  .entry-primary {
    display: flex;
    align-items: baseline;
    gap: 4pt;
    flex-wrap: wrap;
  }
  .entry-title {
    font-weight: 600;
    font-size: 10.5pt;
    color: #111;
  }
  .entry-org {
    font-size: 10pt;
    color: #444;
  }
  .entry-meta {
    font-size: 9pt;
    color: #666;
    white-space: nowrap;
  }
  .bullets {
    list-style: disc;
    padding-left: 16pt;
    margin-top: 3pt;
  }
  .bullets li {
    font-size: 10pt;
    color: #333;
    line-height: 1.55;
    margin-bottom: 2pt;
  }
  .cert-detail {
    font-size: 9pt;
    color: #555;
    margin-top: 2pt;
  }
  .cert-detail a {
    color: #333;
    text-decoration: none;
  }
  .links-list {
    list-style: none;
    display: flex;
    flex-wrap: wrap;
    gap: 8pt 16pt;
  }
  .links-list a {
    font-size: 10pt;
    color: #333;
    text-decoration: none;
  }
  @media print {
    body { background: none; }
    .resume { padding: 0; max-width: none; }
  }
</style>
""";
}
