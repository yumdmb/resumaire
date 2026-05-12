namespace Resumaire.Api.Tailoring;

public interface IAiTailoringSuggestionGenerator
{
    Task<AiTailoringSuggestionGenerationResult> GenerateAsync(
        AiTailoringSuggestionGenerationRequest request,
        CancellationToken cancellationToken);
}
