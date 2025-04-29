using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap;
using Volo.Abp.AspNetCore.VirtualFileSystem;
using Volo.Abp.AspNetCore.Mvc.Libs;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bundling;

[DependsOn(
    typeof(AbpAspNetCoreMvcUiBootstrapModule),
    typeof(AbpAspNetCoreBundlingModule)
)]
public class AbpAspNetCoreMvcUiBundlingModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        if (!context.Services.IsDataMigrationEnvironment())
        {
            Configure<AbpMvcLibsOptions>(options =>
            {
                options.CheckLibs = true;
            });
        }

        context.Services.AddSingleton<IValidateOptions<AbpBundlingOptions>, AbpBundlingGlobalAssetsOptionsValidation>();
        Configure<AbpEndpointRouterOptions>(options =>
        {
            options.EndpointConfigureActions.Add(endpointContext =>
            {
                var abpBundlingOptions = endpointContext.ScopeServiceProvider.GetRequiredService<IOptions<AbpBundlingOptions>>().Value;
                if (!abpBundlingOptions.GlobalAssets.Enabled)
                {
                    return;
                }

                endpointContext.Endpoints.MapGet(abpBundlingOptions.GlobalAssets.CssFileName, async httpContext =>
                {
                    var abpGlobalAssetsBundleService = httpContext.RequestServices.GetRequiredService<IAbpGlobalAssetsBundleService>();
                    var styles = await abpGlobalAssetsBundleService.GetStylesAsync();
                    httpContext.Response.ContentType = "text/css";
                    await httpContext.Response.WriteAsync(styles);
                });

                endpointContext.Endpoints.MapGet(abpBundlingOptions.GlobalAssets.JavaScriptFileName, async httpContext =>
                {
                    var abpGlobalAssetsBundleService = httpContext.RequestServices.GetRequiredService<IAbpGlobalAssetsBundleService>();
                    var scripts = await abpGlobalAssetsBundleService.GetScriptsAsync();
                    httpContext.Response.ContentType = "text/javascript";
                    await httpContext.Response.WriteAsync(scripts);
                });
            });
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var environment = context.GetEnvironmentOrNull();
        if (environment != null)
        {
            environment.WebRootFileProvider =
                new CompositeFileProvider(
                    context.GetEnvironment().WebRootFileProvider,
                    context.ServiceProvider.GetRequiredService<IWebContentFileProvider>()
                );
        }
    }
}
