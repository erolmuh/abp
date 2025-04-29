using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Storage;
using Volo.Abp.AspNetCore.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Volo.Abp.AspNetCore.Components.MauiBlazor.Bundling;

public class AbpGlobalAssetsBundleService : AbpGlobalAssetsBundleServiceBase<AbpGlobalAssetsBundleService>
{
    protected IFileProvider MauiBlazorContentFileProvider { get; }
    protected string RootPath = "/wwwroot";

    public AbpGlobalAssetsBundleService(
        IOptions<AbpBundlingOptions> abpBundlingOptions,
        IBundleManager bundleManager)
        : base(abpBundlingOptions, bundleManager)
    {

        MauiBlazorContentFileProvider = CreateMauiBlazorContentFileProvider();
    }

    protected virtual IFileProvider CreateMauiBlazorContentFileProvider()
    {
        var assetsDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "wwwroot");
        if (!Path.Exists(assetsDirectory))
        {
            Directory.CreateDirectory(assetsDirectory);
        }

        return new PhysicalFileProvider(assetsDirectory);
    }

    protected override IFileInfo? GetFileInfo(string fileName)
    {
        return MauiBlazorContentFileProvider.GetFileInfo(fileName);
    }
}
