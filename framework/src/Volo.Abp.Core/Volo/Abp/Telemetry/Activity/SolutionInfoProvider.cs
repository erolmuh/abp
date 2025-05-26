using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.Shared;
using Volo.Abp.Telemetry.Shared.Enums;

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
            ("template", v => dict[ActivityPropertyName.Template] = MapSolutionTemplate(v.GetString())),
            ("createdAbpStudioVersion", v => dict[ActivityPropertyName.CreatedAbpStudioVersion] = v.GetString()!),
            ("multiTenancy", v => dict[ActivityPropertyName.MultiTenancy] = ParseBool(v)),
            ("uiFramework", v => dict[ActivityPropertyName.UiFramework] = MapUiFramework(v.GetString())),
            ("databaseProvider", v => dict[ActivityPropertyName.DatabaseProvider] = MapDatabaseProvider(v.GetString())),
            ("theme", v => dict[ActivityPropertyName.Theme] = MapUiTheme(v.GetString())),
            ("themeStyle", v => dict[ActivityPropertyName.ThemeStyle] = MapUiThemeStyle(v.GetString())),
            ("publicWebsite", v => dict[ActivityPropertyName.HasPublicWebsite] = ParseBool(v)),
            ("tiered", v => dict[ActivityPropertyName.IsTiered] = ParseBool(v)),
            ("socialLogin", v => dict[ActivityPropertyName.SocialLogins] = ParseBool(v)),
            ("databaseManagementSystem", v => dict[ActivityPropertyName.DatabaseManagementSystem] = MapDbms(v.GetString())),
            ("separateTenantSchema", v => dict[ActivityPropertyName.IsSeparateTenantSchema] = ParseBool(v)),
            ("mobileFramework", v => dict[ActivityPropertyName.MobileFramework] = MapMobileApp(v.GetString())),
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

    protected virtual UiTheme MapUiTheme(string? theme)
    {
        return theme switch
        {
            "none" => UiTheme.None,
            "basic" => UiTheme.Basic,
            "leptonx" => UiTheme.LeptonX,
            "leptonx-lite" => UiTheme.LeptonXLite,
            _ => UiTheme.Unknown
        };
    }

    protected virtual UiThemeStyle MapUiThemeStyle(string? style)
    {
        return style switch
        {
            "dim" => UiThemeStyle.Dim,
            "style" => UiThemeStyle.System,
            "dark" => UiThemeStyle.Dark,
            "light" => UiThemeStyle.Light,
            _ => UiThemeStyle.Unknown
        };
    }

    protected virtual SolutionTemplate MapSolutionTemplate(string? template)
    {
        return template switch
        {
            "microservice" => SolutionTemplate.Microservice,
            "app-nolayers" => SolutionTemplate.AppNoLayers,
            "app" => SolutionTemplate.AppLayered,
            _ => SolutionTemplate.Unknown
        };
    }

    protected virtual UiFramework MapUiFramework(string? framework)
    {
        return framework switch
        {
            "mvc" => UiFramework.MvcRazorPages,
            "blazor" => UiFramework.BlazorWasm,
            "angular" => UiFramework.Angular,
            "blazor-server" => UiFramework.BlazorServer,
            "blazor-webapp" => UiFramework.BlazorWebApp,
            "maui-blazor" => UiFramework.BlazorMaUI,
            "none" => UiFramework.None,
            _ => UiFramework.Unknown
        };
    }

    protected virtual DatabaseProvider MapDatabaseProvider(string? provider)
    {
        return provider switch
        {
            "ef" => DatabaseProvider.EfCore,
            "mongodb" => DatabaseProvider.MongoDb,
            "none" => DatabaseProvider.None,
            _ => DatabaseProvider.Unknown
        };
    }

    protected virtual Dbms MapDbms(string? dbms)
    {
        return dbms switch
        {
            "mysql" => Dbms.MySql,
            "oracle" => Dbms.Oracle,
            "oracle-devart" => Dbms.OracleDevart,
            "postgresql" => Dbms.PostgreSql,
            "sqlserver" => Dbms.SqlServer,
            "sqlite" => Dbms.Sqlite,
            _ => Dbms.Unknown
        };
    }

    protected virtual MobileApp MapMobileApp(string? mobile)
    {
        return mobile switch
        {
            "maui" => MobileApp.Maui,
            "react-native" => MobileApp.ReactNative,
            "none" => MobileApp.None,
            _ => MobileApp.Unknown
        };
    }
}