using Resumaire.Api.Contracts;
using Resumaire.Api.Tailoring;

namespace Resumaire.Api.Tests;

public sealed class TailoringSuggestionGuardrailsTests
{
    private const string JobDescription =
        "We need an engineer with React, TypeScript and Docker experience. Kubernetes is a plus.";

    [Fact]
    public void Validate_AcceptsRewriteOfExistingBullet_AndUsesRealOriginalText()
    {
        var result = Validate(Draft(
            "Experience",
            "Experience[0].Bullets[0]",
            TailoringOperations.Replace,
            "Built a React dashboard used to review API workflows.",
            ["Experience[0].Bullets[0]"],
            originalContent: "invented original text"));

        var accepted = Assert.Single(result.Suggestions);
        Assert.Equal("Built a React dashboard for API workflow review.", accepted.OriginalContent);
        Assert.Empty(result.Rejections);
    }

    [Theory]
    [InlineData("Experience[0].Role")]
    [InlineData("Experience[0].Organization")]
    [InlineData("Education[0].Degree")]
    [InlineData("Certifications[0].Name")]
    public void Validate_RejectsIdentityFieldsAsTargets(string path)
    {
        var section = ResumeContentEditor.SectionOf(path);

        var result = Validate(Draft(
            section,
            path,
            TailoringOperations.Replace,
            "Built a React dashboard used to review API workflows.",
            ["Experience[0].Bullets[0]"]));

        Assert.Empty(result.Suggestions);
        Assert.Contains(result.Rejections, rejection => rejection.Contains("not an editable"));
    }

    [Fact]
    public void Validate_RejectsSectionThatDisagreesWithTargetPath()
    {
        var result = Validate(Draft(
            "Summary",
            "Experience[0].Bullets[0]",
            TailoringOperations.Replace,
            "Built a React dashboard used to review API workflows.",
            ["Experience[0].Bullets[0]"]));

        Assert.Empty(result.Suggestions);
        Assert.Contains(result.Rejections, rejection => rejection.Contains("does not belong to section"));
    }

    [Fact]
    public void Validate_RejectsTargetIndexThatDoesNotExist()
    {
        var result = Validate(Draft(
            "Experience",
            "Experience[4].Bullets[0]",
            TailoringOperations.Replace,
            "Built a React dashboard used to review API workflows.",
            ["Experience[0].Bullets[0]"]));

        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void Validate_AcceptsSentenceInitialVerbBeforeKnownTechnology()
    {
        // "Built React dashboards" looks like a capitalised phrase but the first word is only grammar.
        var result = Validate(Draft(
            "Experience",
            "Experience[0].Bullets",
            TailoringOperations.AddBullet,
            "Built React dashboards that support API workflow review.",
            ["Experience[0].Bullets[0]"]));

        Assert.Single(result.Suggestions);
    }

    [Fact]
    public void Validate_RejectsKeywordTheResumeDoesNotSupport()
    {
        var result = Validate(Draft(
            "Experience",
            "Experience[0].Bullets[0]",
            TailoringOperations.Replace,
            "Built a Docker and React dashboard used to review API workflows.",
            ["Experience[0].Bullets[0]"]));

        Assert.Empty(result.Suggestions);
        Assert.Contains(result.Rejections, rejection => rejection.Contains("unsupported keyword 'docker'"));
    }

    [Fact]
    public void Validate_RejectsInventedMetric_ButAcceptsFormattingVariantsOfResumeMetrics()
    {
        var invented = Validate(Draft(
            "Experience",
            "Experience[1].Bullets[0]",
            TailoringOperations.Replace,
            "Reduced API latency by 90% for React dashboard users.",
            ["Experience[1].Bullets[0]"]));

        var reformatted = Validate(Draft(
            "Experience",
            "Experience[1].Bullets[0]",
            TailoringOperations.Replace,
            "Reduced API latency by 40 % for 1,000 React dashboard users.",
            ["Experience[1].Bullets[0]"]));

        Assert.Empty(invented.Suggestions);
        Assert.Contains(invented.Rejections, rejection => rejection.Contains("unsupported metric '90%'"));
        Assert.Single(reformatted.Suggestions);
    }

    [Fact]
    public void Validate_DoesNotTreatLongerNumberAsMatchingShorterResumeMetric()
    {
        var result = Validate(Draft(
            "Experience",
            "Experience[1].Bullets[0]",
            TailoringOperations.Replace,
            "Reduced API latency by 140% for React dashboard users.",
            ["Experience[1].Bullets[0]"]));

        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void Validate_AcceptsSkillReorder_AndRejectsAddedSkill()
    {
        var reorder = Validate(Draft(
            "Skills",
            "Skills",
            TailoringOperations.SetSkills,
            "React, TypeScript",
            ["Skills[1]"]));

        var added = Validate(Draft(
            "Skills",
            "Skills",
            TailoringOperations.SetSkills,
            "React, TypeScript, Terraform",
            ["Skills[1]"]));

        Assert.Single(reorder.Suggestions);
        Assert.Empty(added.Suggestions);
        Assert.Contains(added.Rejections, rejection => rejection.Contains("'Terraform'"));
    }

    [Fact]
    public void Validate_AcceptsEvidenceThatIsNotLinkedToAJobKeyword()
    {
        // Experience[1] has no keyword evidence, but it is a real resume path and is valid evidence.
        var result = Validate(Draft(
            "Experience",
            "Experience[1].Bullets",
            TailoringOperations.AddBullet,
            "Reduced API latency for React dashboard users.",
            ["Experience[1].Bullets[0]"]));

        Assert.Single(result.Suggestions);
    }

    [Fact]
    public void Validate_RejectsEvidencePathThatDoesNotExist()
    {
        var result = Validate(Draft(
            "Summary",
            "Summary",
            TailoringOperations.Replace,
            "Builds reliable APIs.",
            ["Experience[9].Bullets[9]"]));

        Assert.Empty(result.Suggestions);
        Assert.Contains(result.Rejections, rejection => rejection.Contains("not supported by the base resume"));
    }

    [Fact]
    public void Validate_RejectsOverlongSuggestion()
    {
        var result = Validate(Draft(
            "Summary",
            "Summary",
            TailoringOperations.Replace,
            "Builds reliable APIs. " + new string('x', 1100),
            ["Summary"]));

        Assert.Empty(result.Suggestions);
    }

    private static TailoringSuggestionGuardrailResult Validate(AiTailoringSuggestionDraft draft)
    {
        var resume = Resume();
        var keywords = new JobKeywordExtractor().Extract(JobDescription).Keywords;
        var comparison = new ResumeKeywordComparer().Compare(keywords, resume);

        return new TailoringSuggestionGuardrails().Validate(
            new AiTailoringSuggestionGenerationResult([draft], []),
            resume,
            comparison);
    }

    private static AiTailoringSuggestionDraft Draft(
        string section,
        string targetPath,
        string operation,
        string suggested,
        IReadOnlyList<string> evidence,
        string? originalContent = null) =>
        new(
            section,
            originalContent,
            suggested,
            "It restates content already on the resume.",
            evidence,
            null,
            targetPath,
            operation);

    private static ResumeContentDto Resume() =>
        new(
            new ResumePersonalInfoDto("Ada Lovelace", "ada@example.test", null, "London", "Backend Engineer", null),
            "Builds reliable APIs with PostgreSQL-backed services.",
            ["React", "TypeScript"],
            [
                new ResumeExperienceDto(
                    "exp-1",
                    "Backend Engineer",
                    "Example Co",
                    "Remote",
                    "2024-01",
                    null,
                    true,
                    ["Built a React dashboard for API workflow review."]),
                new ResumeExperienceDto(
                    "exp-2",
                    "Developer",
                    "Other Co",
                    "Remote",
                    "2022-01",
                    "2023-12",
                    false,
                    ["Reduced API latency by 40% for 1000 dashboard users."])
            ],
            [],
            [new ResumeCertificationDto("cert-1", "Cloud Basics", "Provider", null, null, null, null)],
            []);
}
