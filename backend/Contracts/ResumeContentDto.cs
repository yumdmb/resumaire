namespace Resumaire.Api.Contracts;

public static class ResumeContentSchema
{
    public const int CurrentVersion = 1;
}

public sealed record ResumeContentDto(
    ResumePersonalInfoDto? PersonalInfo,
    string? Summary,
    IReadOnlyList<string>? Skills,
    IReadOnlyList<ResumeExperienceDto>? Experience,
    IReadOnlyList<ResumeEducationDto>? Education,
    IReadOnlyList<ResumeCertificationDto>? Certifications,
    IReadOnlyList<ResumeLinkDto>? Links);

public sealed record ResumePersonalInfoDto(
    string? FullName,
    string? Email,
    string? Phone,
    string? Location,
    string? Headline,
    string? Website);

public sealed record ResumeExperienceDto(
    string? Id,
    string? Role,
    string? Organization,
    string? Location,
    string? StartDate,
    string? EndDate,
    bool IsCurrent,
    IReadOnlyList<string>? Bullets);

public sealed record ResumeEducationDto(
    string? Id,
    string? Institution,
    string? Degree,
    string? Field,
    string? Location,
    string? StartDate,
    string? EndDate,
    IReadOnlyList<string>? Details);

public sealed record ResumeCertificationDto(
    string? Id,
    string? Name,
    string? Issuer,
    string? IssuedDate,
    string? ExpirationDate,
    string? CredentialId,
    string? Url);

public sealed record ResumeLinkDto(
    string? Id,
    string? Label,
    string? Url);
