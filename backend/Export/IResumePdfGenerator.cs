namespace Resumaire.Api.Export;

public interface IResumePdfGenerator
{
    Task<byte[]> GenerateAsync(string html, CancellationToken cancellationToken = default);
}
