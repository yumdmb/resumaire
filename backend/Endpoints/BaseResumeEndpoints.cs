using System.Net.Mail;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Contracts;
using Resumaire.Api.Data;
using Resumaire.Api.Data.Entities;
using Resumaire.Api.Infrastructure.Api;
using Resumaire.Api.Infrastructure.Auth;

namespace Resumaire.Api.Endpoints;

public static class BaseResumeEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static RouteGroupBuilder MapBaseResumeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/resume/base")
            .RequireAuthorization()
            .WithTags("Resume Profile");

        group.MapGet("", GetBaseResumeAsync)
            .WithName("GetBaseResume");

        group.MapPut("", SaveBaseResumeAsync)
            .WithName("SaveBaseResume");

        return group;
    }

    private static async Task<IResult> GetBaseResumeAsync(
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        var resume = await dbContext.BaseResumes
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);

        return resume is null
            ? Results.NotFound()
            : ApiResponses.Ok(MapBaseResumeResponse(resume));
    }

    private static async Task<IResult> SaveBaseResumeAsync(
        SaveBaseResumeRequest request,
        ApplicationDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateResumeContent(request.Content);
        if (validationErrors.Count > 0)
        {
            return validationErrors.ToValidationProblem();
        }

        var userId = httpContext.User.GetUserId();
        var now = DateTimeOffset.UtcNow;
        var contentJson = JsonSerializer.Serialize(request.Content, JsonOptions);
        var resume = await dbContext.BaseResumes
            .SingleOrDefaultAsync(value => value.UserId == userId, cancellationToken);

        if (resume is null)
        {
            resume = new BaseResume
            {
                UserId = userId,
                SchemaVersion = ResumeContentSchema.CurrentVersion,
                Revision = 1,
                ContentJson = contentJson,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.BaseResumes.Add(resume);
        }
        else
        {
            resume.SchemaVersion = ResumeContentSchema.CurrentVersion;
            resume.Revision += 1;
            resume.ContentJson = contentJson;
            resume.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponses.Ok(MapBaseResumeResponse(resume));
    }

    private static BaseResumeResponse MapBaseResumeResponse(BaseResume resume)
    {
        var content = JsonSerializer.Deserialize<ResumeContentDto>(resume.ContentJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored base resume content is empty.");

        return new BaseResumeResponse(
            resume.Id,
            resume.SchemaVersion,
            resume.Revision,
            content,
            resume.CreatedAt,
            resume.UpdatedAt);
    }

    private static Dictionary<string, string[]> ValidateResumeContent(ResumeContentDto? content)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (content is null)
        {
            AddValidationError(errors, nameof(SaveBaseResumeRequest.Content), "Resume content is required.");
            return errors;
        }

        ValidatePersonalInfo(errors, content.PersonalInfo);
        ValidateOptionalText(errors, nameof(content.Summary), content.Summary, maxLength: 4000);
        ValidateRequiredCollection(errors, nameof(content.Skills), content.Skills, maxItems: 100);
        ValidateRequiredCollection(errors, nameof(content.Experience), content.Experience, maxItems: 50);
        ValidateRequiredCollection(errors, nameof(content.Education), content.Education, maxItems: 50);
        ValidateRequiredCollection(errors, nameof(content.Certifications), content.Certifications, maxItems: 50);
        ValidateRequiredCollection(errors, nameof(content.Links), content.Links, maxItems: 50);

        if (content.Skills is not null)
        {
            for (var index = 0; index < content.Skills.Count; index++)
            {
                ValidateRequiredText(errors, $"{nameof(content.Skills)}[{index}]", content.Skills[index], maxLength: 100);
            }
        }

        if (content.Experience is not null)
        {
            for (var index = 0; index < content.Experience.Count; index++)
            {
                ValidateExperience(errors, content.Experience[index], $"{nameof(content.Experience)}[{index}]");
            }
        }

        if (content.Education is not null)
        {
            for (var index = 0; index < content.Education.Count; index++)
            {
                ValidateEducation(errors, content.Education[index], $"{nameof(content.Education)}[{index}]");
            }
        }

        if (content.Certifications is not null)
        {
            for (var index = 0; index < content.Certifications.Count; index++)
            {
                ValidateCertification(errors, content.Certifications[index], $"{nameof(content.Certifications)}[{index}]");
            }
        }

        if (content.Links is not null)
        {
            for (var index = 0; index < content.Links.Count; index++)
            {
                ValidateLink(errors, content.Links[index], $"{nameof(content.Links)}[{index}]");
            }
        }

        return errors;
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

public sealed record SaveBaseResumeRequest(ResumeContentDto? Content);

public sealed record BaseResumeResponse(
    Guid Id,
    int SchemaVersion,
    int Revision,
    ResumeContentDto Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
