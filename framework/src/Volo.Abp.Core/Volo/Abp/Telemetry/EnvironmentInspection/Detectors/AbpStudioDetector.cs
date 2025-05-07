using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Shared;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class AbpStudioDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Abp Studio";

    public override Task<SoftwareInfo?> DetectAsync()
    {
        try
        {
            var uiTheme = GetAbpStudioUiTheme();
            var version = GetAbpStudioVersion();

            return Task.FromResult<SoftwareInfo?>(new SoftwareInfo(Name, version, uiTheme, SoftwareType.AbpStudio));
        }
        catch (Exception e)
        {
            return Task.FromResult<SoftwareInfo?>(null);
        }
    }

    private string? GetAbpStudioUiTheme()
    {
        var ideStateJsonPath = Path.Combine(
            AbpTelemetryPaths.Studio,
            "ui",
            "ide-state.json"
        );
        if (!File.Exists(ideStateJsonPath))
        {
            return null;
        }

        var json = File.ReadAllText(ideStateJsonPath);
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.TryGetProperty("theme", out var themeElement) ? themeElement.GetString() : null;
    }

    private string? GetAbpStudioVersion()
    {
        var extensionsFilePath = Path.Combine(AbpTelemetryPaths.Studio, "extensions.json");

        if (!File.Exists(extensionsFilePath))
        {
            return null;
        }

        var json = File.ReadAllText(extensionsFilePath);
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.TryGetProperty("version", out var versionElement) ? versionElement.GetString() : null;
    }
}