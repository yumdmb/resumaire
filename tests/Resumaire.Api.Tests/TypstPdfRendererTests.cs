using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Resumaire.Api.Configuration;
using Resumaire.Api.Contracts;
using Resumaire.Api.Export;

namespace Resumaire.Api.Tests;

public sealed class TypstPdfRendererTests
{
    [Fact]
    public void Map_BuildsTypstDataFromResumeContent()
    {
        var data = TypstResumeDataMapper.Map(SampleContent());

        Assert.Equal("Ada Lovelace", data.Name);
        Assert.Equal(
            ["+1 555 0100", "London", "ada@example.test", "example.test/ada", "Portfolio"],
            data.Contact.Select(item => item.Text));
        Assert.Equal("tel:+15550100", data.Contact[0].Url);
        Assert.Equal("mailto:ada@example.test", data.Contact[2].Url);
        Assert.Equal("2020 -- Present", data.Experience[0].Dates);
        Assert.Equal("BSc, Mathematics", data.Education[0].Degree);
        Assert.Equal("2018 -- 2020", data.Education[0].Dates);
        Assert.Equal("C#, Typst", data.Skills.Single().Items);
        Assert.Equal("", data.Skills.Single().Category);
        Assert.Equal("Engine", data.Projects.Single().Name);
    }

    [Fact]
    public void Map_PrefersSkillGroupsOverFlatSkills()
    {
        var content = SampleContent() with
        {
            SkillGroups = [new ResumeSkillGroupDto("Languages", ["C#", "Rust"])]
        };

        var group = Assert.Single(TypstResumeDataMapper.Map(content).Skills);

        Assert.Equal("Languages", group.Category);
        Assert.Equal("C#, Rust", group.Items);
    }

    [Fact]
    public void ToDataJson_KeepsSpecialCharactersAsPlainStrings()
    {
        var content = SampleContent() with
        {
            Summary = "Uses #set, @ref, $x$, *bold* and \\ backslashes & more."
        };

        using var document = JsonDocument.Parse(TypstResumeDataMapper.ToDataJson(content));

        Assert.Equal(
            "Uses #set, @ref, $x$, *bold* and \\ backslashes & more.",
            document.RootElement.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task RenderAsync_WithTypstInstalled_ProducesPdf()
    {
        if (!IsTypstAvailable())
        {
            return; // Real compile only runs where the typst CLI is installed.
        }

        var renderer = CreateRenderer();
        var content = SampleContent() with
        {
            Summary = "Uses #set, @ref, $x$, *bold* and \\ backslashes & more."
        };

        var pdf = await renderer.RenderAsync(content);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(pdf.Length > 1000);

        var cached = await renderer.RenderAsync(content);
        Assert.Same(pdf, cached);
    }

    [Fact]
    public async Task RenderAsync_WhenExecutableMissing_ThrowsResumeRenderException()
    {
        var renderer = CreateRenderer("typst-executable-that-does-not-exist");

        await Assert.ThrowsAsync<ResumeRenderException>(() => renderer.RenderAsync(SampleContent()));
    }

    private static TypstPdfRenderer CreateRenderer(string executable = "typst") =>
        new(
            Options.Create(new TypstOptions { Executable = executable }),
            new MemoryCache(new MemoryCacheOptions { SizeLimit = 10L * 1024 * 1024 }),
            NullLogger<TypstPdfRenderer>.Instance);

    internal static bool IsTypstAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("typst", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process!.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static ResumeContentDto SampleContent() =>
        new(
            new ResumePersonalInfoDto(
                "Ada Lovelace",
                "ada@example.test",
                "+1 555 0100",
                "London",
                "Engineer",
                "https://example.test/ada"),
            "Builds reliable APIs.",
            ["C#", "Typst"],
            [
                new ResumeExperienceDto(
                    "exp-1",
                    "Engineer",
                    "Analytical Engines",
                    "London",
                    "2020",
                    null,
                    true,
                    ["Shipped the first program."])
            ],
            [
                new ResumeEducationDto(
                    "edu-1",
                    "University of London",
                    "BSc",
                    "Mathematics",
                    "London",
                    "2018",
                    "2020",
                    [])
            ],
            [new ResumeCertificationDto("cert-1", "Cert", "Issuer", "2024", null, null, null)],
            [new ResumeLinkDto("link-1", "Portfolio", "https://example.test/portfolio")],
            Projects: [new ResumeProjectDto("proj-1", "Engine", "https://example.test/engine", "C#, .NET", ["Built it."])],
            Activities: [new ResumeActivityDto("act-1", "Hackathon", "Online", "Participant", "2024", ["Won."])]);
}
