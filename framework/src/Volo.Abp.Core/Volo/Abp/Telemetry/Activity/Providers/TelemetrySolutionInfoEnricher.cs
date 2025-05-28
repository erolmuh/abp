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

  

    public async Task EnrichAsync(ActivityData activity)
    {
        var solutionId = GetSolutionId(activity);
        if (solutionId.HasValue)
        {
            if (await _telemetryActivityStorage.ShouldAddSolutionInformation(solutionId.Value))
            {
                await FillSolutionInfoAsync(activity);
                await _telemetryActivityStorage.MarkSolutionInfoAsAddedAsync(solutionId.Value);
            }

            activity.Remove(ActivityPropertyName.SolutionPath);
        }
    }

    private Guid? GetSolutionId(ActivityData activity)
    {
        if (TryGetSolutionIdFromActivity(activity, out var solutionId))
        {
            return solutionId;
        }

        if (TryGetSolutionIdFromFile(activity, out var idFromFile))
        {
            activity[ActivityPropertyName.SolutionId] = idFromFile;
            return idFromFile;
        }

        return null;
    }
    private async Task FillSolutionInfoAsync(ActivityData activityData)
    {
        try
        {
            if (!activityData.TryGetValue(ActivityPropertyName.SolutionPath, out var rawSolutionPath)
                || string.IsNullOrWhiteSpace(rawSolutionPath?.ToString())
                || !File.Exists(rawSolutionPath?.ToString()))
            {
                return;
            }

            var solutionPath = rawSolutionPath!.ToString();

            if (!File.Exists(solutionPath))
            {
                return;
            }

            var jsonContent = File.ReadAllText(solutionPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            AddSolutionInformation(activityData, root.GetProperty("creatingStudioConfiguration"));

            activityData[ActivityPropertyName.LicenseType] = await GetLicenseTypeAsync();

            if (root.TryGetProperty("modules", out var modulesElement) &&
                modulesElement.ValueKind == JsonValueKind.Object)
            {
                var directoryPath = Path.GetDirectoryName(solutionPath)!;
                activityData[ActivityPropertyName.InstalledModules] = ExtractModules(directoryPath, modulesElement);
            }
        }
        catch
        {
            // ignored
        }
    }

    private bool TryGetSolutionIdFromActivity(ActivityData activity, out Guid solutionId)
    {
        solutionId = Guid.Empty;

        if (activity.TryGetValue(ActivityPropertyName.SolutionId, out var value) &&
            Guid.TryParse(value?.ToString(), out var parsedId))
        {
            solutionId = parsedId;
            return true;
        }

        return false;
    }

    private bool TryGetSolutionIdFromFile(ActivityData activity, out Guid solutionId)
    {
        solutionId = Guid.Empty;

        if (!activity.TryGetValue(ActivityPropertyName.SolutionPath, out var value))
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
            // ignore malformed json etc.
        }

        return false;
    }

    private void AddSolutionInformation(Dictionary<string, object> activity, JsonElement config)
    {
        var map = new (string Key, Action<JsonElement> Apply)[]
        {
            ("template", v => activity[ActivityPropertyName.Template] = v.GetString() ?? string.Empty),
            ("createdAbpStudioVersion", v => activity[ActivityPropertyName.CreatedAbpStudioVersion] = v.GetString()!),
            ("multiTenancy", v => activity[ActivityPropertyName.MultiTenancy] = ParseBool(v)),
            ("uiFramework", v => activity[ActivityPropertyName.UiFramework] = v.GetString() ?? string.Empty),
            ("databaseProvider", v => activity[ActivityPropertyName.DatabaseProvider] = v.GetString() ?? string.Empty),
            ("theme", v => activity[ActivityPropertyName.Theme] = v.GetString() ?? string.Empty),
            ("themeStyle", v => activity[ActivityPropertyName.ThemeStyle] = v.GetString() ?? string.Empty),
            ("publicWebsite", v => activity[ActivityPropertyName.HasPublicWebsite] = ParseBool(v)),
            ("tiered", v => activity[ActivityPropertyName.IsTiered] = ParseBool(v)),
            ("socialLogin", v => activity[ActivityPropertyName.SocialLogins] = ParseBool(v)),
            ("databaseManagementSystem", v => activity[ActivityPropertyName.DatabaseManagementSystem] = v.GetString() ?? string.Empty),
            ("separateTenantSchema", v => activity[ActivityPropertyName.IsSeparateTenantSchema] = ParseBool(v)),
            ("mobileFramework", v => activity[ActivityPropertyName.MobileFramework] = v.GetString() ?? string.Empty),
            ("includeTests", v => activity[ActivityPropertyName.IncludeTests] = ParseBool(v)),
            ("dynamicLocalization", v => activity[ActivityPropertyName.DynamicLocalization] = ParseBool(v)),
            ("kubernetesConfiguration", v => activity[ActivityPropertyName.KubernetesConfiguration] = ParseBool(v)),
            ("grafanaDashboard", v => activity[ActivityPropertyName.GrafanaDashboard] = ParseBool(v)),
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
                        x.TryGetValue(ActivityPropertyName.ModuleName, out var n) && n as string == import.Name &&
                        x.TryGetValue(ActivityPropertyName.ModuleVersion, out var v) && v as string == version))
                {
                    continue;
                }

                var moduleEntry = new Dictionary<string, object> { { ActivityPropertyName.ModuleName, import.Name } };
                if (!version.IsNullOrEmpty())
                {
                    moduleEntry[ActivityPropertyName.ModuleVersion] = version;
                }

                if (creationTime.HasValue)
                {
                    moduleEntry[ActivityPropertyName.ModuleInstallationTime] = creationTime.Value;
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

    private async Task<int> GetLicenseTypeAsync()
    {
        if (!File.Exists(TelemetryPaths.AccessToken))
        {
            return 0;
        }

        try
        {
            using var httpClient = new HttpClient();
            var accessToken = File.ReadAllText(TelemetryPaths.AccessToken);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync($"{AbpPlatformUrls.AbpIoUrl}api/license/api-key");
            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("licenseType").GetInt32();
        }
        catch
        {
            return 0;
        }
    }
}