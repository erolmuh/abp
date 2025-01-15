using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling.Scripts;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling.Styles;
using Volo.Abp.DependencyInjection;
using Volo.Abp.VirtualFileSystem;

namespace Volo.Abp.AspNetCore.Components.MauiBlazor.Bundling;

public class BundleManager : BundleManagerBase, ITransientDependency
{
    protected IMauiBlazorContentFileProvider MauiBlazorContentFileProvider { get; }

    public BundleManager(
        IOptions<AbpBundlingOptions> options,
        IOptions<AbpBundleContributorOptions> contributorOptions,
        IScriptBundler scriptBundler,
        IStyleBundler styleBundler,
        IServiceProvider serviceProvider,
        IDynamicFileProvider dynamicFileProvider,
        IBundleCache bundleCache,
        IMauiBlazorContentFileProvider mauiBlazorContentFileProvider) : base(
        options,
        contributorOptions,
        scriptBundler,
        styleBundler,
        serviceProvider,
        dynamicFileProvider,
        bundleCache)
    {
        MauiBlazorContentFileProvider = mauiBlazorContentFileProvider;
    }

    public override bool IsBundlingEnabled()
    {
        switch (Options.Mode)
        {
            case BundlingMode.None:
                return false;
            case BundlingMode.Bundle:
            case BundlingMode.BundleAndMinify:
                return true;
            case BundlingMode.Auto:
                return !IsDebug();
            default:
                throw new AbpException($"Unhandled {nameof(BundlingMode)}: {Options.Mode}");
        }
    }

    protected override bool IsMinficationEnabled()
    {
        switch (Options.Mode)
        {
            case BundlingMode.None:
            case BundlingMode.Bundle:
                return false;
            case BundlingMode.BundleAndMinify:
                return true;
            case BundlingMode.Auto:
                return !IsDebug();
            default:
                throw new AbpException($"Unhandled {nameof(BundlingMode)}: {Options.Mode}");
        }
    }

    protected virtual bool IsDebug()
    {
        #if DEBUG
                return true;
        #else
                retur false;
        #endif
    }

    protected override IFileProvider GetFileProvider()
    {
        return MauiBlazorContentFileProvider;
    }
}
