using System;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Maui.Storage;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;
using Volo.Abp.VirtualFileSystem;

namespace Volo.Abp.AspNetCore.Components.MauiBlazor.Bundling;

public class MauiBlazorContentFileProvider : IMauiBlazorContentFileProvider, ISingletonDependency
{
    protected IAbpGlobalAssetsBundleService AbpGlobalAssetsBundleService { get; }
    protected IOptions<AbpBundlingOptions> AbpBundlingOptions { get; }

    public MauiBlazorContentFileProvider(
        IAbpGlobalAssetsBundleService abpGlobalAssetsBundleService,
        IOptions<AbpBundlingOptions> abpBundlingOptions)
    {
        AbpGlobalAssetsBundleService = abpGlobalAssetsBundleService;
        AbpBundlingOptions = abpBundlingOptions;
    }

    public string ContentRootPath => FileSystem.Current.AppDataDirectory;

    public IFileInfo GetFileInfo(string subpath)
    {
        if (string.IsNullOrEmpty(subpath))
        {
            return new NotFoundFileInfo(subpath);
        }

        if (string.Equals(subpath, AbpBundlingOptions.Value.GlobalAssets.GlobalStyleBundleName!, StringComparison.OrdinalIgnoreCase))
        {
            var styles = AsyncHelper.RunSync(() => AbpGlobalAssetsBundleService.GetStylesAsync());
            return new InMemoryFileInfo(subpath, Encoding.UTF8.GetBytes(styles), AbpBundlingOptions.Value.GlobalAssets.GlobalStyleBundleName!);
        }

        if (string.Equals(subpath, AbpBundlingOptions.Value.GlobalAssets.GlobalScriptBundleName!, StringComparison.OrdinalIgnoreCase))
        {
            var scripts = AsyncHelper.RunSync(() => AbpGlobalAssetsBundleService.GetScriptsAsync());
            return new InMemoryFileInfo(subpath, Encoding.UTF8.GetBytes(scripts), AbpBundlingOptions.Value.GlobalAssets.GlobalScriptBundleName!);
        }

        return new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return NotFoundDirectoryContents.Singleton;
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }
}
