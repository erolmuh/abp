using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetrySolutionInfoEnricher : ITelemetryActivityEventEnricher, ISingletonDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    public TelemetrySolutionInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }


    public async Task EnrichAsync(ActivityEvent activity)
    {
        if (TryGetSolutionId(activity, out var solutionId))
        {
            if (await _telemetryActivityStorage.ShouldAddSolutionInformation(solutionId))
            {
                activity[ActivityPropertyNames.SolutionId] = solutionId;
                AddSolutionInfo(activity);
                await _telemetryActivityStorage.MarkSolutionInfoAsAddedAsync(solutionId);
                activity[ActivityPropertyNames.HasSolutionInfo] = true;
                activity.Remove(ActivityPropertyNames.SolutionPath);
            }
        }
    }

    private bool TryGetSolutionId(ActivityEvent activity, out Guid solutionId)
    {
        if (TryGetSolutionIdFromActivity(activity, out solutionId))
        {
            return true;
        }

        if (TryGetSolutionIdFromFile(activity, out solutionId))
        {
            return true;
        }

        solutionId = Guid.Empty;
        return false;
    }

    private void AddSolutionInfo(ActivityEvent activity)
    {
        try
        {
            var solutionPath = activity[ActivityPropertyNames.SolutionPath]!.ToString()!;

            var jsonContent = File.ReadAllText(solutionPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            AddSolutionCreationConfiguration(activity, root.GetProperty("creatingStudioConfiguration"));

            if (root.TryGetProperty("modules", out var modulesElement) &&
                modulesElement.ValueKind == JsonValueKind.Object)
            {
                activity[ActivityPropertyNames.InstalledModules] = ExtractModules(solutionPath, modulesElement);
            }
        }
        catch
        {
            // ignored
        }
    }

    private bool TryGetSolutionIdFromActivity(ActivityEvent activity, out Guid solutionId)
    {
        if (activity.TryGetValue(ActivityPropertyNames.SolutionId, out var value) &&
            Guid.TryParse(value?.ToString(), out var parsedId))
        {
            solutionId = parsedId;
            return true;
        }

        solutionId = Guid.Empty;
        return false;
    }

    private bool TryGetSolutionIdFromFile(ActivityEvent activity, out Guid solutionId)
    {
        if (activity.TryGetValue(ActivityPropertyNames.SolutionPath, out var pathValue) &&
            pathValue is string &&
            File.Exists(pathValue.ToString()))
        {
            try
            {
                var solutionPath = pathValue.ToString()!;
                var json = File.ReadAllText(solutionPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("id", out var idProp) &&
                    Guid.TryParse(idProp.GetString(), out var parsedId))
                {
                    solutionId = parsedId;
                    return true;
                }
            }
            catch
            {
                // ignored
            }
        }

        solutionId = Guid.Empty;
        return false;
    }

    private void AddSolutionCreationConfiguration(ActivityEvent activity, JsonElement config)
    {
        activity[ActivityPropertyNames.Template] = config.GetProperty("template").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.CreatedAbpStudioVersion] = config.GetProperty("createdAbpStudioVersion").GetString()!;
        activity[ActivityPropertyNames.MultiTenancy] = ParseBool(config.GetProperty("multiTenancy"));
        activity[ActivityPropertyNames.UiFramework] = config.GetProperty("uiFramework").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.DatabaseProvider] = config.GetProperty("databaseProvider").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.Theme] = config.GetProperty("theme").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.ThemeStyle] = config.GetProperty("themeStyle").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.HasPublicWebsite] = ParseBool(config.GetProperty("publicWebsite"));
        activity[ActivityPropertyNames.IsTiered] = ParseBool(config.GetProperty("tiered"));
        activity[ActivityPropertyNames.SocialLogins] = ParseBool(config.GetProperty("socialLogin"));
        activity[ActivityPropertyNames.DatabaseManagementSystem] = config.GetProperty("databaseManagementSystem").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.IsSeparateTenantSchema] = ParseBool(config.GetProperty("separateTenantSchema"));
        activity[ActivityPropertyNames.MobileFramework] = config.GetProperty("mobileFramework").GetString() ?? string.Empty;
        activity[ActivityPropertyNames.IncludeTests] = ParseBool(config.GetProperty("includeTests"));
        activity[ActivityPropertyNames.DynamicLocalization] = ParseBool(config.GetProperty("dynamicLocalization"));
        activity[ActivityPropertyNames.KubernetesConfiguration] = ParseBool(config.GetProperty("kubernetesConfiguration"));
        activity[ActivityPropertyNames.GrafanaDashboard] = ParseBool(config.GetProperty("grafanaDashboard"));
    }

    private List<Dictionary<string, object>> ExtractModules(string solutionPath, JsonElement modulesElement)
    {
        var modules = new List<Dictionary<string, object>>();

        foreach (var module in modulesElement.EnumerateObject())
        {
            var modulePath = GetModuleFilePath(solutionPath, module);
            if (modulePath.IsNullOrEmpty())
            {
                continue;
            }

            var moduleJson = File.ReadAllText(modulePath);
            using var moduleDoc = JsonDocument.Parse(moduleJson);

            if (!moduleDoc.RootElement.TryGetProperty("imports", out var imports) ||
                imports.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var import in imports.EnumerateObject())
            {
                var version = import.Value.GetProperty("version").GetString();

                var creationTime = import.Value.TryGetProperty("creationTime", out var ct)
                    ? DateTimeOffset.Parse(ct.GetString()!)
                    : (DateTimeOffset?)null;

                if (modules.Any(x =>
                        x.TryGetValue(ActivityPropertyNames.ModuleName, out var n) && n as string == import.Name &&
                        x.TryGetValue(ActivityPropertyNames.ModuleVersion, out var v) && v as string == version))
                {
                    continue;
                }

                var moduleEntry = new Dictionary<string, object> { { ActivityPropertyNames.ModuleName, import.Name } };
                if (!version.IsNullOrEmpty())
                {
                    moduleEntry[ActivityPropertyNames.ModuleVersion] = version;
                }

                if (creationTime.HasValue)
                {
                    moduleEntry[ActivityPropertyNames.ModuleInstallationTime] = creationTime.Value;
                }

                modules.Add(moduleEntry);
            }
        }

        return modules;
    }

    private string? GetModuleFilePath(string solutionPath, JsonProperty module)
    {
        if (!module.Value.TryGetProperty("path", out var pathElement))
        {
            return null;
        }

        var path = pathElement.GetString();
        if (path.IsNullOrEmpty())
        {
            return null;
        }

        var fullPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, path);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private bool ParseBool(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var b) => b,
            _ => false
        };
    }
}