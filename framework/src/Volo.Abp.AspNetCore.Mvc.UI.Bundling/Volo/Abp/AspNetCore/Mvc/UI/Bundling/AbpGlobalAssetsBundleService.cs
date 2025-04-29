using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Bundling;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bundling;

public class AbpGlobalAssetsBundleService :  AbpGlobalAssetsBundleServiceBase<AbpGlobalAssetsBundleService>
{
    protected IWebHostEnvironment WebHostEnvironment { get; }

    public AbpGlobalAssetsBundleService(
        IOptions<AbpBundlingOptions> abpBundlingOptions,
        IBundleManager bundleManager,
        IWebHostEnvironment webHostEnvironment)
        : base(abpBundlingOptions, bundleManager)
    {
        WebHostEnvironment = webHostEnvironment;
    }

    protected override IFileInfo? GetFileInfo(string fileName)
    {
        return WebHostEnvironment.WebRootFileProvider?.GetFileInfo(fileName);
    }
}
