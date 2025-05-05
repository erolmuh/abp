using System;
using System.IO;
using System.Threading.Tasks;
using EnvironmentInspection.Contracts;
using EnvironmentInspection.Enums;
using Newtonsoft.Json.Linq;

namespace EnvironmentInspection.Detectors;

internal class AbpStudioDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Abp Studio";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        try
        {
            var uiTheme = await GetAbpStudioUiThemeAsync();
            var version = await GetAbpStudioVersionAsync();

            return new SoftwareInfo(Name, version, uiTheme, SoftwareType.AbpStudio);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public async Task<string?> GetAbpStudioUiThemeAsync()
    {
        var ideStateJsonPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".abp",
            "studio",
            "ui",
            "ide-state.json"
        );

        var jObject = JObject.Parse(await File.ReadAllTextAsync(ideStateJsonPath));
        return jObject["theme"]?.Value<string>();
    }

    public async Task<string?> GetAbpStudioVersionAsync()
    {
        var extensionsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".abp",
            "studio",
            "extensions.json"
        );

        var jObject = JObject.Parse(await File.ReadAllTextAsync(extensionsFilePath));

        return jObject["version"]?.Value<string>();
    }
}

