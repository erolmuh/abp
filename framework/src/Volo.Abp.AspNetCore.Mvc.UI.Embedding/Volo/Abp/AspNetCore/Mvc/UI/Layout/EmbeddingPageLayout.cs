using Microsoft.AspNetCore.Http;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.AspNetCore.Mvc.UI.Layout;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IPageLayout))]
public class EmbeddingPageLayout : PageLayout, IScopedDependency
{
    protected IPageEmbeddingService PageEmbeddingService { get; }
    protected IHttpContextAccessor HttpContextAccessor { get; }
    
    private bool? _renderLayoutElements;

    public EmbeddingPageLayout(IPageEmbeddingService pageEmbeddingService, IHttpContextAccessor httpContextAccessor)
    {
        PageEmbeddingService = pageEmbeddingService;
        HttpContextAccessor = httpContextAccessor;
    }

    public ContentLayout Content { get; } = new();

    public override bool RenderLayoutElements 
    { 
        get
        {
            if (_renderLayoutElements.HasValue)
            {
                return _renderLayoutElements.Value;
            }

            // Check if this is an embedding request
            var httpContext = HttpContextAccessor.HttpContext;
            if (httpContext != null && PageEmbeddingService.IsEmbeddingRequest(httpContext))
            {
                return false;
            }

            return true;
        }
        set => _renderLayoutElements = value;
    }
}
