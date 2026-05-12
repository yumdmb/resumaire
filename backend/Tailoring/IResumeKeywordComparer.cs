using Resumaire.Api.Contracts;

namespace Resumaire.Api.Tailoring;

public interface IResumeKeywordComparer
{
    ResumeKeywordComparisonResult Compare(
        IReadOnlyList<JobKeyword> jobKeywords,
        ResumeContentDto resumeContent);
}
