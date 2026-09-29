using Resumaire.Api.Contracts;
using Resumaire.Api.Tailoring;

namespace Resumaire.Api.Tests;

public sealed class ResumeContentEditorTests
{
    [Fact]
    public void Apply_Replace_ChangesOnlyTheTargetedBullet()
    {
        var updated = ResumeContentEditor.Apply(
            Resume(),
            "Experience[0].Bullets[1]",
            TailoringOperations.Replace,
            "Second, rewritten.");

        Assert.Equal(["First.", "Second, rewritten."], updated.Experience![0].Bullets);
        Assert.Equal("Engineer", updated.Experience[0].Role);
        Assert.Equal("Example Co", updated.Experience[0].Organization);
        Assert.Equal(["Project bullet."], updated.Projects![0].Bullets);
    }

    [Fact]
    public void Apply_AddBullet_AppendsToTheTargetedList()
    {
        var updated = ResumeContentEditor.Apply(
            Resume(),
            "Projects[0].Bullets",
            TailoringOperations.AddBullet,
            "New project bullet.");

        Assert.Equal(["Project bullet.", "New project bullet."], updated.Projects![0].Bullets);
        Assert.Equal(["First.", "Second."], updated.Experience![0].Bullets);
    }

    [Fact]
    public void Apply_SetSkills_ReordersAndNeverAddsSkills()
    {
        var updated = ResumeContentEditor.Apply(
            Resume(),
            "Skills",
            TailoringOperations.SetSkills,
            "TypeScript, Terraform, React");

        Assert.Equal(["TypeScript", "React"], updated.Skills);
    }

    [Fact]
    public void Apply_ReplaceSummaryAndHeadline()
    {
        var content = Resume();

        var withSummary = ResumeContentEditor.Apply(content, "Summary", TailoringOperations.Replace, "New summary.");
        var withHeadline = ResumeContentEditor.Apply(content, "PersonalInfo.Headline", TailoringOperations.Replace, "New headline");

        Assert.Equal("New summary.", withSummary.Summary);
        Assert.Equal("New headline", withHeadline.PersonalInfo!.Headline);
        Assert.Equal("Ada Lovelace", withHeadline.PersonalInfo.FullName);
    }

    [Theory]
    [InlineData("Experience[0].Role", TailoringOperations.Replace)]
    [InlineData("Experience[5].Bullets[0]", TailoringOperations.Replace)]
    [InlineData("Experience[0].Bullets[9]", TailoringOperations.Replace)]
    [InlineData("Experience[0].Bullets[0]", TailoringOperations.AddBullet)]
    [InlineData("Skills[0]", TailoringOperations.SetSkills)]
    [InlineData("Links[0].Label", TailoringOperations.Replace)]
    [InlineData("Summary", "Delete")]
    public void TryValidateTarget_RejectsNonEditableOrInconsistentTargets(string path, string operation)
    {
        var valid = ResumeContentEditor.TryValidateTarget(Resume(), path, operation, out _, out var error);

        Assert.False(valid);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void TryValidateTarget_ReturnsCurrentTextAsOriginal()
    {
        var valid = ResumeContentEditor.TryValidateTarget(
            Resume(),
            "Activities[0].Bullets[0]",
            TailoringOperations.Replace,
            out var original,
            out _);

        Assert.True(valid);
        Assert.Equal("Organized the event.", original);
    }

    [Theory]
    [InlineData("Experience[0].Role", "Engineer")]
    [InlineData("Projects[0].Technologies", "C#, .NET")]
    [InlineData("Skills[1]", "TypeScript")]
    public void TryReadText_ResolvesKnownPaths(string path, string expected)
    {
        Assert.True(ResumeContentEditor.TryReadText(Resume(), path, out var text));
        Assert.Equal(expected, text);
    }

    [Fact]
    public void Apply_IgnoresBlankText()
    {
        var content = Resume();

        Assert.Same(content, ResumeContentEditor.Apply(content, "Summary", TailoringOperations.Replace, "   "));
    }

    private static ResumeContentDto Resume() =>
        new(
            new ResumePersonalInfoDto("Ada Lovelace", null, null, null, "Old headline", null),
            "Old summary.",
            ["React", "TypeScript"],
            [new ResumeExperienceDto("e1", "Engineer", "Example Co", null, null, null, false, ["First.", "Second."])],
            [],
            [],
            [],
            Projects: [new ResumeProjectDto("p1", "Engine", null, "C#, .NET", ["Project bullet."])],
            Activities: [new ResumeActivityDto("a1", "Hackathon", null, "Lead", null, ["Organized the event."])]);
}
