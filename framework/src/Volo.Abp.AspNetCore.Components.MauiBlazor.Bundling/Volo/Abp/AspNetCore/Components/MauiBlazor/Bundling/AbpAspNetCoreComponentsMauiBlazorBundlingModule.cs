using Volo.Abp.AspNetCore.Bundling;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace Volo.Abp.AspNetCore.Components.MauiBlazor.Bundling;

[DependsOn(
    typeof(AbpAspNetCoreComponentsMauiBlazorModule),
    typeof(AbpAspNetCoreBundlingModule)
)]
public class AbpAspNetCoreComponentsMauiBlazorBundlingModule : AbpModule
{
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(() => OnApplicationInitializationAsync(context));
    }
}
