namespace Resumaire.Api.Tailoring;

public interface IJobKeywordExtractor
{
    JobKeywordExtractionResult Extract(string jobDescription);
}
