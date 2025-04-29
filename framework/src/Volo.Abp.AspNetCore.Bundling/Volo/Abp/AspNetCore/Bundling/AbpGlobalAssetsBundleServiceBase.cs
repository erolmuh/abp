using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.Bundling.Styles;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.AspNetCore.Bundling;

public abstract class AbpGlobalAssetsBundleServiceBase<TGlobalAssetsBundleService> : IAbpGlobalAssetsBundleService, ITransientDependency
    where TGlobalAssetsBundleService : class, IAbpGlobalAssetsBundleService
{
    public ILogger<TGlobalAssetsBundleService> Logger { get; set; }

    protected IOptions<AbpBundlingOptions> AbpBundlingOptions { get; }
    protected IBundleManager BundleManager { get; }

    protected AbpGlobalAssetsBundleServiceBase(
        IOptions<AbpBundlingOptions> abpBundlingOptions,
        IBundleManager bundleManager)
    {
        AbpBundlingOptions = abpBundlingOptions;
        BundleManager = bundleManager;

        Logger = NullLogger<TGlobalAssetsBundleService>.Instance;
    }

    public async Task<string> GetStylesAsync()
    {
        var styleFiles = await BundleManager.GetStyleBundleFilesAsync(AbpBundlingOptions.Value.GlobalAssets.GlobalStyleBundleName!);
        var styles = string.Empty;

        foreach (var file in styleFiles)
        {
            var fileInfo = GetFileInfo(file.FileName);
            if (fileInfo == null || !fileInfo.Exists)
            {
                Logger.LogError($"Could not find the file: {file.FileName}");
                continue;
            }

            var fileContent = await fileInfo.ReadAsStringAsync();
            if (!BundleManager.As<BundleManagerBase>().IsBundlingEnabled())
            {
                fileContent = CssRelativePath.Adjust(fileContent,
                    file.FileName,
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));

                styles += $"/*{file.FileName}*/{Environment.NewLine}{fileContent}{Environment.NewLine}{Environment.NewLine}";
            }
            else
            {
                styles += $"{fileContent}{Environment.NewLine}{Environment.NewLine}";
            }
        }

        return styles;
    }

    public virtual async Task<string> GetScriptsAsync()
    {
        var scriptFiles = await BundleManager.GetScriptBundleFilesAsync(AbpBundlingOptions.Value.GlobalAssets.GlobalScriptBundleName!);
        var scripts = string.Empty;

        foreach (var file in scriptFiles)
        {
            var fileInfo = GetFileInfo(file.FileName);
            if (fileInfo == null || !fileInfo.Exists)
            {
                Logger.LogError($"Could not find the file: {file.FileName}");
                continue;
            }

            var fileContent = await fileInfo.ReadAsStringAsync();
            if (!BundleManager.As<BundleManagerBase>().IsBundlingEnabled())
            {
                scripts += $"{fileContent.EnsureEndsWith(';')}{Environment.NewLine}{Environment.NewLine}";
            }
            else
            {
                scripts += $"//{file.FileName}{Environment.NewLine}{fileContent.EnsureEndsWith(';')}{Environment.NewLine}{Environment.NewLine}";
            }
        }

        return scripts;
    }

    protected abstract IFileInfo? GetFileInfo(string fileName);
}
