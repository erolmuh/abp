using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityDataEnricher))]
public class TelemetrySolutionInfoEnricher : ITelemetryActivityDataEnricher, ISingletonDependency
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
                await FillSolutionInfoAsync(activity);
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

    private Task FillSolutionInfoAsync(ActivityEvent activity)
    {
        try
        {
            if (!activity.TryGetValue(ActivityPropertyNames.SolutionPath, out var rawSolutionPath)
                || string.IsNullOrWhiteSpace(rawSolutionPath?.ToString())
                || !File.Exists(rawSolutionPath?.ToString()))
            {
                return Task.CompletedTask;
            }

            var solutionPath = rawSolutionPath!.ToString()!;
            var jsonContent = File.ReadAllText(solutionPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            AddSolutionInformation(activity, root.GetProperty("creatingStudioConfiguration"));

            if (root.TryGetProperty("modules", out var modulesElement) && modulesElement.ValueKind == JsonValueKind.Object)
            {
                var directoryPath = Path.GetDirectoryName(solutionPath)!;
                activity[ActivityPropertyNames.InstalledModules] = ExtractModules(directoryPath, modulesElement);
            }
        }
        catch
        {
            // ignored
        }

        return Task.CompletedTask;
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
        solutionId = Guid.Empty;
        
        if (!activity.TryGetValue(ActivityPropertyNames.SolutionPath, out var value))
        {
            return false;
        }

        var path = value?.ToString();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
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
        
        return false;
    }

    private void AddSolutionInformation(Dictionary<string, object> activity, JsonElement config)
    {
        var map = new (string Key, Action<JsonElement> Apply)[]
        {
            ("template", v => activity[ActivityPropertyNames.Template] = v.GetString() ?? string.Empty),
            ("createdAbpStudioVersion", v => activity[ActivityPropertyNames.CreatedAbpStudioVersion] = v.GetString()!),
            ("multiTenancy", v => activity[ActivityPropertyNames.MultiTenancy] = ParseBool(v)),
            ("uiFramework", v => activity[ActivityPropertyNames.UiFramework] = v.GetString() ?? string.Empty),
            ("databaseProvider", v => activity[ActivityPropertyNames.DatabaseProvider] = v.GetString() ?? string.Empty),
            ("theme", v => activity[ActivityPropertyNames.Theme] = v.GetString() ?? string.Empty),
            ("themeStyle", v => activity[ActivityPropertyNames.ThemeStyle] = v.GetString() ?? string.Empty),
            ("publicWebsite", v => activity[ActivityPropertyNames.HasPublicWebsite] = ParseBool(v)),
            ("tiered", v => activity[ActivityPropertyNames.IsTiered] = ParseBool(v)),
            ("socialLogin", v => activity[ActivityPropertyNames.SocialLogins] = ParseBool(v)),
            ("databaseManagementSystem", v => activity[ActivityPropertyNames.DatabaseManagementSystem] = v.GetString() ?? string.Empty),
            ("separateTenantSchema", v => activity[ActivityPropertyNames.IsSeparateTenantSchema] = ParseBool(v)),
            ("mobileFramework", v => activity[ActivityPropertyNames.MobileFramework] = v.GetString() ?? string.Empty),
            ("includeTests", v => activity[ActivityPropertyNames.IncludeTests] = ParseBool(v)),
            ("dynamicLocalization", v => activity[ActivityPropertyNames.DynamicLocalization] = ParseBool(v)),
            ("kubernetesConfiguration", v => activity[ActivityPropertyNames.KubernetesConfiguration] = ParseBool(v)),
            ("grafanaDashboard", v => activity[ActivityPropertyNames.GrafanaDashboard] = ParseBool(v)),
        };

        foreach (var (key, apply) in map)
        {
            if (config.TryGetProperty(key, out var prop))
            {
                apply(prop);
            }
        }
    }

    private List<Dictionary<string, object>> ExtractModules(string directoryPath, JsonElement modulesElement)
    {
        var modules = new List<Dictionary<string, object>>();

        foreach (var module in modulesElement.EnumerateObject())
        {
            if (!module.Value.TryGetProperty("path", out var pathElement))
            {
                continue;
            }

            var path = pathElement.GetString();
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var fullPath = Path.Combine(directoryPath, path);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var moduleJson = File.ReadAllText(fullPath);
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