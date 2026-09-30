using System.Net.Mail;

namespace Resumaire.Api.Contracts;

/// <summary>
/// Shared validation for resume content, used by base resume saves, tailored version saves and exports.
/// Collections introduced in schema v2 (projects, activities, skill groups) are optional so that
/// v1 content and older clients keep working.
/// </summary>
public static class ResumeContentValidator
{
    public static Dictionary<string, string[]> Validate(ResumeContentDto? content, string rootField = "Content")
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (content is null)
        {
            AddValidationError(errors, rootField, "Resume content is required.");
            return errors;
        }

        ValidatePersonalInfo(errors, content.PersonalInfo);
        ValidateOptionalText(errors, nameof(content.Summary), content.Summary, maxLength: 4000);
        ValidateRequiredCollection(errors, nameof(content.Skills), content.Skills, maxItems: 100);
        ValidateRequiredCollection(errors, nameof(content.Experience), content.Experience, maxItems: 50);
        ValidateRequiredCollection(errors, nameof(content.Education), content.Education, maxItems: 50);
        ValidateRequiredCollection(errors, nameof(content.Certifications), content.Certifications, maxItems: 50);
        ValidateRequiredCollection(errors, nameof(content.Links), content.Links, maxItems: 50);
        ValidateOptionalCollection(errors, nameof(content.Projects), content.Projects, maxItems: 50);
        ValidateOptionalCollection(errors, nameof(content.Activities), content.Activities, maxItems: 50);
        ValidateOptionalCollection(errors, nameof(content.SkillGroups), content.SkillGroups, maxItems: 30);

        if (content.Skills is not null)
        {
            for (var index = 0; index < content.Skills.Count; index++)
            {
                ValidateRequiredText(errors, $"{nameof(content.Skills)}[{index}]", content.Skills[index], maxLength: 100);
            }
        }

        ForEach(content.Experience, nameof(content.Experience), (item, path) => ValidateExperience(errors, item, path));
        ForEach(content.Education, nameof(content.Education), (item, path) => ValidateEducation(errors, item, path));
        ForEach(content.Certifications, nameof(content.Certifications), (item, path) => ValidateCertification(errors, item, path));
        ForEach(content.Links, nameof(content.Links), (item, path) => ValidateLink(errors, item, path));
        ForEach(content.Projects, nameof(content.Projects), (item, path) => ValidateProject(errors, item, path));
        ForEach(content.Activities, nameof(content.Activities), (item, path) => ValidateActivity(errors, item, path));
        ForEach(content.SkillGroups, nameof(content.SkillGroups), (item, path) => ValidateSkillGroup(errors, item, path));

        return errors;
    }

    private static void ForEach<T>(IReadOnlyList<T>? items, string name, Action<T, string> validate)
    {
        if (items is null)
        {
            return;
        }

        for (var index = 0; index < items.Count; index++)
        {
            validate(items[index], $"{name}[{index}]");
        }
    }

    private static void ValidatePersonalInfo(
        Dictionary<string, string[]> errors,
        ResumePersonalInfoDto? personalInfo)
    {
        if (personalInfo is null)
        {
            AddValidationError(errors, nameof(ResumeContentDto.PersonalInfo), "Personal info is required.");
            return;
        }

        ValidateRequiredText(errors, "PersonalInfo.FullName", personalInfo.FullName, maxLength: 200);
        ValidateOptionalText(errors, "PersonalInfo.Phone", personalInfo.Phone, maxLength: 50);
        ValidateOptionalText(errors, "PersonalInfo.Location", personalInfo.Location, maxLength: 200);
        ValidateOptionalText(errors, "PersonalInfo.Headline", personalInfo.Headline, maxLength: 200);
        ValidateOptionalUri(errors, "PersonalInfo.Website", personalInfo.Website);

        if (!string.IsNullOrWhiteSpace(personalInfo.Email))
        {
            if (personalInfo.Email.Trim().Length > 320)
            {
                AddValidationError(errors, "PersonalInfo.Email", "Email cannot exceed 320 characters.");
            }

            try
            {
                _ = new MailAddress(personalInfo.Email.Trim());
            }
            catch (FormatException)
            {
                AddValidationError(errors, "PersonalInfo.Email", "Email must be a valid email address.");
            }
        }
    }

    private static void ValidateExperience(
        Dictionary<string, string[]> errors,
        ResumeExperienceDto? experience,
        string path)
    {
        if (experience is null)
        {
            AddValidationError(errors, path, "Experience item is required.");
            return;
        }

        ValidateOptionalText(errors, $"{path}.Id", experience.Id, maxLength: 100);
        ValidateRequiredText(errors, $"{path}.Role", experience.Role, maxLength: 200);
        ValidateRequiredText(errors, $"{path}.Organization", experience.Organization, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Location", experience.Location, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.StartDate", experience.StartDate, maxLength: 50);
        ValidateOptionalText(errors, $"{path}.EndDate", experience.EndDate, maxLength: 50);
        ValidateDetails(errors, $"{path}.Bullets", experience.Bullets);
    }

    private static void ValidateEducation(
        Dictionary<string, string[]> errors,
        ResumeEducationDto? education,
        string path)
    {
        if (education is null)
        {
            AddValidationError(errors, path, "Education item is required.");
            return;
        }

        ValidateOptionalText(errors, $"{path}.Id", education.Id, maxLength: 100);
        ValidateRequiredText(errors, $"{path}.Institution", education.Institution, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Degree", education.Degree, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Field", education.Field, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Location", education.Location, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.StartDate", education.StartDate, maxLength: 50);
        ValidateOptionalText(errors, $"{path}.EndDate", education.EndDate, maxLength: 50);
        ValidateDetails(errors, $"{path}.Details", education.Details);
    }

    private static void ValidateCertification(
        Dictionary<string, string[]> errors,
        ResumeCertificationDto? certification,
        string path)
    {
        if (certification is null)
        {
            AddValidationError(errors, path, "Certification item is required.");
            return;
        }

        ValidateOptionalText(errors, $"{path}.Id", certification.Id, maxLength: 100);
        ValidateRequiredText(errors, $"{path}.Name", certification.Name, maxLength: 200);
        ValidateRequiredText(errors, $"{path}.Issuer", certification.Issuer, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.IssuedDate", certification.IssuedDate, maxLength: 50);
        ValidateOptionalText(errors, $"{path}.ExpirationDate", certification.ExpirationDate, maxLength: 50);
        ValidateOptionalText(errors, $"{path}.CredentialId", certification.CredentialId, maxLength: 200);
        ValidateOptionalUri(errors, $"{path}.Url", certification.Url);
    }

    private static void ValidateLink(
        Dictionary<string, string[]> errors,
        ResumeLinkDto? link,
        string path)
    {
        if (link is null)
        {
            AddValidationError(errors, path, "Link item is required.");
            return;
        }

        ValidateOptionalText(errors, $"{path}.Id", link.Id, maxLength: 100);
        ValidateRequiredText(errors, $"{path}.Label", link.Label, maxLength: 100);
        ValidateRequiredUri(errors, $"{path}.Url", link.Url);
    }

    private static void ValidateProject(
        Dictionary<string, string[]> errors,
        ResumeProjectDto? project,
        string path)
    {
        if (project is null)
        {
            AddValidationError(errors, path, "Project item is required.");
            return;
        }

        ValidateOptionalText(errors, $"{path}.Id", project.Id, maxLength: 100);
        ValidateRequiredText(errors, $"{path}.Name", project.Name, maxLength: 200);
        ValidateOptionalUri(errors, $"{path}.Url", project.Url);
        ValidateOptionalText(errors, $"{path}.Technologies", project.Technologies, maxLength: 300);
        ValidateDetails(errors, $"{path}.Bullets", project.Bullets);
    }

    private static void ValidateActivity(
        Dictionary<string, string[]> errors,
        ResumeActivityDto? activity,
        string path)
    {
        if (activity is null)
        {
            AddValidationError(errors, path, "Activity item is required.");
            return;
        }

        ValidateOptionalText(errors, $"{path}.Id", activity.Id, maxLength: 100);
        ValidateRequiredText(errors, $"{path}.Title", activity.Title, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Location", activity.Location, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Role", activity.Role, maxLength: 200);
        ValidateOptionalText(errors, $"{path}.Date", activity.Date, maxLength: 50);
        ValidateDetails(errors, $"{path}.Bullets", activity.Bullets);
    }

    private static void ValidateSkillGroup(
        Dictionary<string, string[]> errors,
        ResumeSkillGroupDto? group,
        string path)
    {
        if (group is null)
        {
            AddValidationError(errors, path, "Skill group is required.");
            return;
        }

        ValidateRequiredText(errors, $"{path}.Category", group.Category, maxLength: 100);

        if (group.Items is null)
        {
            AddValidationError(errors, $"{path}.Items", $"{path}.Items is required.");
            return;
        }

        if (group.Items.Count > 100)
        {
            AddValidationError(errors, $"{path}.Items", $"{path}.Items cannot contain more than 100 items.");
        }

        for (var index = 0; index < group.Items.Count; index++)
        {
            ValidateRequiredText(errors, $"{path}.Items[{index}]", group.Items[index], maxLength: 100);
        }
    }

    private static void ValidateDetails(
        Dictionary<string, string[]> errors,
        string path,
        IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            AddValidationError(errors, path, $"{path} is required.");
            return;
        }

        if (values.Count > 30)
        {
            AddValidationError(errors, path, $"{path} cannot contain more than 30 items.");
        }

        for (var index = 0; index < values.Count; index++)
        {
            ValidateRequiredText(errors, $"{path}[{index}]", values[index], maxLength: 1000);
        }
    }

    private static void ValidateRequiredCollection<T>(
        Dictionary<string, string[]> errors,
        string fieldName,
        IReadOnlyCollection<T>? values,
        int maxItems)
    {
        if (values is null)
        {
            AddValidationError(errors, fieldName, $"{fieldName} is required.");
            return;
        }

        if (values.Count > maxItems)
        {
            AddValidationError(errors, fieldName, $"{fieldName} cannot contain more than {maxItems} items.");
        }
    }

    private static void ValidateOptionalCollection<T>(
        Dictionary<string, string[]> errors,
        string fieldName,
        IReadOnlyCollection<T>? values,
        int maxItems)
    {
        if (values is not null && values.Count > maxItems)
        {
            AddValidationError(errors, fieldName, $"{fieldName} cannot contain more than {maxItems} items.");
        }
    }

    private static void ValidateRequiredText(
        Dictionary<string, string[]> errors,
        string fieldName,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddValidationError(errors, fieldName, $"{fieldName} is required.");
            return;
        }

        ValidateOptionalText(errors, fieldName, value, maxLength);
    }

    private static void ValidateOptionalText(
        Dictionary<string, string[]> errors,
        string fieldName,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            AddValidationError(errors, fieldName, $"{fieldName} cannot exceed {maxLength} characters.");
        }
    }

    private static void ValidateRequiredUri(
        Dictionary<string, string[]> errors,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddValidationError(errors, fieldName, $"{fieldName} is required.");
            return;
        }

        ValidateOptionalUri(errors, fieldName, value);
    }

    private static void ValidateOptionalUri(
        Dictionary<string, string[]> errors,
        string fieldName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > 2048)
        {
            AddValidationError(errors, fieldName, $"{fieldName} cannot exceed 2048 characters.");
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            AddValidationError(errors, fieldName, $"{fieldName} must be an absolute HTTP or HTTPS URL.");
        }
    }

    private static void AddValidationError(
        Dictionary<string, string[]> errors,
        string fieldName,
        string message)
    {
        if (errors.TryGetValue(fieldName, out var messages))
        {
            errors[fieldName] = [.. messages, message];
            return;
        }

        errors[fieldName] = [message];
    }
}
