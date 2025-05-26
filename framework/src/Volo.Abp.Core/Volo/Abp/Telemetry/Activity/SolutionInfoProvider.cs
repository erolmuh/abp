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
            var dictionary = new Dictionary<string, object>();

            var solutionJson = File.ReadAllText(solutionPath);
            using var solutionDoc = JsonDocument.Parse(solutionJson);
            var root = solutionDoc.RootElement;


            if (root.TryGetProperty("creatingStudioConfiguration", out var config))
            {
                AddCreatingStudioConfiguration(dictionary, config);
            }

            dictionary[ActivityPropertyName.LicenseType] = await GetLicenseTypeAsync();

            if (root.TryGetProperty("modules", out var modulesElement) &&
                modulesElement.ValueKind == JsonValueKind.Object)
            {
                var directoryPath = Path.GetDirectoryName(solutionPath);
                var modules = ExtractModules(directoryPath!, modulesElement);
                dictionary[ActivityPropertyName.InstalledModules] = modules;
            }

            return dictionary;
        }
        catch (Exception)
        {
            return new Dictionary<string, object>();
        }
    }

    public Guid? GetSolutionId(string solutionPath)
    {
        var solutionJson = File.ReadAllText((string)solutionPath);
        using var solutionDoc = JsonDocument.Parse(solutionJson);
        var root = solutionDoc.RootElement;

        return ReadSolutionIdFromJson(root);
    }

    private Guid? ReadSolutionIdFromJson(JsonElement root)
    {
        return root.TryGetProperty("id", out var idElement) && Guid.TryParse(idElement.GetString(), out var id)
            ? id
            : null;
    }

    protected virtual void AddCreatingStudioConfiguration(Dictionary<string, object> dict, JsonElement config)
    {
        var mappings = new Dictionary<string, Action<JsonElement>>
        {
            { "template", value => dict[ActivityPropertyName.Template] = MapSolutionTemplate(value.GetString()) },
            { "createdAbpStudioVersion", value => dict[ActivityPropertyName.CreatedAbpStudioVersion] = value.GetString()! },
            { "multiTenancy", value => dict[ActivityPropertyName.MultiTenancy] = ParseBool(value) },
            { "uiFramework", value => dict[ActivityPropertyName.UiFramework] = MapUiFramework(value.GetString()) },
            { "databaseProvider", value => dict[ActivityPropertyName.DatabaseProvider] = MapDatabaseProvider(value.GetString()) },
            { "theme", value => dict[ActivityPropertyName.Theme] = MapUiTheme(value.GetString()) },
            { "themeStyle", value => dict[ActivityPropertyName.ThemeStyle] = MapUiThemeStyle(value.GetString()) },
            { "publicWebsite", value => dict[ActivityPropertyName.HasPublicWebsite] = ParseBool(value) },
            { "tiered", value => dict[ActivityPropertyName.IsTiered] = ParseBool(value) },
            { "socialLogin", value => dict[ActivityPropertyName.SocialLogins] = ParseBool(value) },
            { "databaseManagementSystem", value => dict[ActivityPropertyName.DatabaseManagementSystem] = MapDbms(value.GetString()) },
            { "separateTenantSchema", value => dict[ActivityPropertyName.IsSeparateTenantSchema] = ParseBool(value) },
            { "mobileFramework", value => dict[ActivityPropertyName.MobileFramework] = MapMobileApp(value.GetString()) },
            { "includeTests", value => dict[ActivityPropertyName.IncludeTests] = ParseBool(value) },
            { "dynamicLocalization", value => dict[ActivityPropertyName.DynamicLocalization] = ParseBool(value) },
            { "kubernetesConfiguration", value => dict[ActivityPropertyName.KubernetesConfiguration] = ParseBool(value) },
            { "grafanaDashboard", value => dict[ActivityPropertyName.GrafanaDashboard] = ParseBool(value) },
        };
        foreach (var mapping in mappings)
        {
            if (config.TryGetProperty(mapping.Key, out var propertyValue))
            {
                mapping.Value(propertyValue);
            }
        }
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


    protected virtual List<Dictionary<string,object>> ExtractModules(string directoryPath, JsonElement modulesElement)
    {
        var modules = new List<Dictionary<string,object>>();

        foreach (var module in modulesElement.EnumerateObject())
        {
            var name = module.Name;
            if (!module.Value.TryGetProperty("path", out var pathElement))
            {
                continue;
            }

            var path = pathElement.GetString();
            if (path.IsNullOrEmpty())
            {
                continue;
            }
            var modulePath = Path.Combine(directoryPath, path);

            if (!File.Exists(modulePath))
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
                        x.ContainsKey("ModuleName") && (string)x["ModuleName"] == name &&
                        x.ContainsKey("Version") && (string)x["Version"] == version))
                {
                    continue;
                }

                if (modules.Any(x =>
                        x.TryGetValue("ModuleName", out var moduleNameObj) && moduleNameObj as string == name &&
                        x.TryGetValue("Version", out var versionObj) && versionObj as string == version))
                {
                    continue;
                }

                modules.Add(new Dictionary<string, object>
                {
                    { "ModuleName", name },
                    { "Version", version ?? string.Empty },
                    { "InstallationTime", creationTime ?? DateTime.MinValue }
                });
            }
        }

        return modules;
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

            var httpResponse = await httpClient.GetAsync($"{AbpPlatformUrls.AbpIoUrl}api/license/api-key");
            if (!httpResponse.IsSuccessStatusCode)
            {
                return 0;
            }

            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            return doc.RootElement.GetProperty("licenseType").GetInt32();
        }
        catch
        {
            return 0;
        }
    }

    protected virtual UiTheme MapUiTheme(string? theme) => theme switch
    {
        "none" => UiTheme.None,
        "basic" => UiTheme.Basic,
        "leptonx" => UiTheme.LeptonX,
        "leptonx-lite" => UiTheme.LeptonXLite,
        _ => UiTheme.Unknown
    };

    protected virtual UiThemeStyle MapUiThemeStyle(string? style) => style switch
    {
        "dim" => UiThemeStyle.Dim,
        "style" => UiThemeStyle.System,
        "dark" => UiThemeStyle.Dark,
        "light" => UiThemeStyle.Light,
        _ => UiThemeStyle.Unknown
    };

    protected virtual SolutionTemplate MapSolutionTemplate(string? template) => template switch
    {
        "microservice" => SolutionTemplate.Microservice,
        "app-nolayers" => SolutionTemplate.AppNoLayers,
        "app" => SolutionTemplate.AppLayered,
        _ => SolutionTemplate.Unknown
    };

    protected virtual UiFramework MapUiFramework(string? framework) => framework switch
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

    protected virtual DatabaseProvider MapDatabaseProvider(string? provider) => provider switch
    {
        "ef" => DatabaseProvider.EfCore,
        "mongodb" => DatabaseProvider.MongoDb,
        "none" => DatabaseProvider.None,
        _ => DatabaseProvider.Unknown
    };

    protected virtual Dbms MapDbms(string? dbms) => dbms switch
    {
        "mysql" => Dbms.MySql,
        "oracle" => Dbms.Oracle,
        "oracle-devart" => Dbms.OracleDevart,
        "postgresql" => Dbms.PostgreSql,
        "sqlserver" => Dbms.SqlServer,
        "sqlite" => Dbms.Sqlite,
        _ => Dbms.Unknown
    };

    protected virtual MobileApp MapMobileApp(string? mobile) => mobile switch
    {
        "maui" => MobileApp.Maui,
        "react-native" => MobileApp.ReactNative,
        "none" => MobileApp.None,
        _ => MobileApp.Unknown
    };
}