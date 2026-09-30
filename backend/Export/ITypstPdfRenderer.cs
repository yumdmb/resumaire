using Resumaire.Api.Contracts;

namespace Resumaire.Api.Export;

public interface ITypstPdfRenderer
{
    Task<byte[]> RenderAsync(ResumeContentDto content, CancellationToken cancellationToken = default);
}

public sealed class ResumeRenderException(string message, Exception? innerException = null)
    : Exception(message, innerException);
