using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Shared;

namespace Volo.Abp.Telemetry.Activity;

public class SolutionInfoProvider : ISolutionInfoProvider, ISingletonDependency
{
    public async Task<IDictionary<string, object>> GetSolutionInfoAsync(string solutionPath)
    {
        try
        {
            var result = new Dictionary<string, object>();
            var jsonContent = File.ReadAllText(solutionPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            AddCreatingStudioConfiguration(result, root.GetProperty("creatingStudioConfiguration"));
            result[ActivityPropertyName.LicenseType] = await GetLicenseTypeAsync();

            if (root.TryGetProperty("modules", out var modulesElement) && modulesElement.ValueKind == JsonValueKind.Object)
            {
                var directoryPath = Path.GetDirectoryName(solutionPath)!;
                result[ActivityPropertyName.InstalledModules] = ExtractModules(directoryPath, modulesElement);
            }

            return result;
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    public Guid? GetSolutionId(string solutionPath)
    {
        var jsonContent = File.ReadAllText(solutionPath);
        using var doc = JsonDocument.Parse(jsonContent);
        return TryGetGuid(doc.RootElement, "id");
    }

    private Guid? TryGetGuid(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var prop) && Guid.TryParse(prop.GetString(), out var id) ? id : null;
    }

    private void AddCreatingStudioConfiguration(Dictionary<string, object> dict, JsonElement config)
    {
        var map = new (string Key, Action<JsonElement> Apply)[]
        {
            ("template", v => dict[ActivityPropertyName.Template] = v.GetString() ?? string.Empty),
            ("createdAbpStudioVersion", v => dict[ActivityPropertyName.CreatedAbpStudioVersion] = v.GetString()!),
            ("multiTenancy", v => dict[ActivityPropertyName.MultiTenancy] = ParseBool(v)),
            ("uiFramework", v => dict[ActivityPropertyName.UiFramework] = v.GetString() ?? string.Empty),
            ("databaseProvider", v => dict[ActivityPropertyName.DatabaseProvider] = v.GetString() ?? string.Empty),
            ("theme", v => dict[ActivityPropertyName.Theme] = v.GetString() ?? string.Empty),
            ("themeStyle", v => dict[ActivityPropertyName.ThemeStyle] = v.GetString() ?? string.Empty),
            ("publicWebsite", v => dict[ActivityPropertyName.HasPublicWebsite] = ParseBool(v)),
            ("tiered", v => dict[ActivityPropertyName.IsTiered] = ParseBool(v)),
            ("socialLogin", v => dict[ActivityPropertyName.SocialLogins] = ParseBool(v)),
            ("databaseManagementSystem", v => dict[ActivityPropertyName.DatabaseManagementSystem] = v.GetString() ?? string.Empty),
            ("separateTenantSchema", v => dict[ActivityPropertyName.IsSeparateTenantSchema] = ParseBool(v)),
            ("mobileFramework", v => dict[ActivityPropertyName.MobileFramework] = v.GetString() ?? string.Empty),
            ("includeTests", v => dict[ActivityPropertyName.IncludeTests] = ParseBool(v)),
            ("dynamicLocalization", v => dict[ActivityPropertyName.DynamicLocalization] = ParseBool(v)),
            ("kubernetesConfiguration", v => dict[ActivityPropertyName.KubernetesConfiguration] = ParseBool(v)),
            ("grafanaDashboard", v => dict[ActivityPropertyName.GrafanaDashboard] = ParseBool(v)),
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

            if (!moduleDoc.RootElement.TryGetProperty("imports", out var imports) || imports.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var import in imports.EnumerateObject())
            {
                var version = import.Value.GetProperty("version").GetString();
                var creationTime = import.Value.TryGetProperty("creationTime", out var ct) ? DateTimeOffset.Parse(ct.GetString()!) : (DateTimeOffset?)null;

                if (modules.Any(x =>
                        x.TryGetValue(ActivityPropertyName.ModuleName, out var n) && n as string == module.Name &&
                        x.TryGetValue(ActivityPropertyName.ModuleVersion, out var v) && v as string == version))
                {
                    continue;
                }

                var moduleEntry = new Dictionary<string, object> { { ActivityPropertyName.ModuleName, module.Name } };
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

    private static bool ParseBool(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var b) => b,
            _ => false
        };
    }

    protected virtual async Task<int> GetLicenseTypeAsync()
    {
        if (!File.Exists(AbpTelemetryPaths.AccessToken))
        {
            return 0;
        }

        try
        {
            using var httpClient = new HttpClient();
            var accessToken = File.ReadAllText(AbpTelemetryPaths.AccessToken);
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