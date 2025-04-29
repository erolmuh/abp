using Newtonsoft.Json.Linq;
using Volo.Abp.Cli;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

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

    private async Task<string?> GetAbpStudioUiThemeAsync()
    {
        var ideStateJsonPath = Path.Combine(
            CliPaths.AbpRootPath,
            "studio",
            "ui",
            "ide-state.json"
        );

        var jObject = JObject.Parse(await File.ReadAllTextAsync(ideStateJsonPath));
        return jObject["theme"]?.Value<string>();
    }

    private async Task<string?> GetAbpStudioVersionAsync()
    {
        var extensionsFilePath = Path.Combine(
            CliPaths.AbpRootPath,
            "studio",
            "extensions.json"
        );

        var jObject = JObject.Parse(await File.ReadAllTextAsync(extensionsFilePath));

        return jObject["version"]?.Value<string>();
    }
}