using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc.UI.Layout;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace Volo.Abp.AspNetCore.Mvc.UI;

[DependsOn(typeof(AbpAspNetCoreMvcUiModule))]
public class AbpAspNetCoreMvcUiEmbeddingModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AbpAspNetCoreMvcUiEmbeddingModule>();
        });

        // Configure page embedding options
        Configure<PageEmbeddingOptions>(options =>
        {
            // Default configuration - can be overridden by applications
        });
    }
}
