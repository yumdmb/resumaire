using Resumaire.Api.Contracts;

namespace Resumaire.Api.Export;

public interface IResumeHtmlRenderer
{
    string RenderToHtml(ResumeContentDto content);
}
