using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.EnvironmentInspection.Core;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class AbpStudioDetector : SoftwareDetector
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
        catch
        {
            return Task.FromResult<SoftwareInfo?>(null);
        }
    }

    private string? GetAbpStudioUiTheme()
    {
        var ideStateJsonPath = Path.Combine(
            TelemetryPaths.Studio,
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
        var extensionsFilePath = Path.Combine(TelemetryPaths.Studio, "extensions.json");

        if (!File.Exists(extensionsFilePath))
        {
            return null;
        }

        var json = File.ReadAllText(extensionsFilePath);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("Extensions", out var extensionsElement) &&
            extensionsElement.ValueKind == JsonValueKind.Array &&
            extensionsElement.GetArrayLength() > 0)
        {
            var firstExtension = extensionsElement[0];
            if (firstExtension.TryGetProperty("version", out var versionElement))
            {
                return versionElement.GetString();
            }
        }

        return null;
    }
}