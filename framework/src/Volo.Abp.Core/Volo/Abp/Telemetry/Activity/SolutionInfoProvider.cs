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

public class SolutionInfoProvider : ISolutionInfoProvider , ISingletonDependency
{
    public async Task<IDictionary<string, object>> GetSolutionInfoAsync(string solutionPath)
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

        if (root.TryGetProperty("modules", out var modulesElement) && modulesElement.ValueKind == JsonValueKind.Object)
        {
            var modules = ExtractModules(modulesElement);
            dictionary[ActivityPropertyName.InstalledModules] = modules;
        }

        return dictionary;
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

    protected virtual void AddCreatingStudioConfiguration(Dictionary<string,object> dict, JsonElement config)
    {
        try
        {
            var mappings = new Dictionary<string, Action<JsonElement>>
            {
                { "template", value => dict[ActivityPropertyName.Template] = MapSolutionTemplate(value.GetString()) },
                { "createdAbpStudioVersion", value => dict[ActivityPropertyName.CreatedAbpStudioVersion] = value.GetString()! },
                { "tiered", value => dict[ActivityPropertyName.IsTiered] = ParseBool(value) },
                { "uiFramework", value => dict[ActivityPropertyName.UiFramework] = MapUiFramework(value.GetString()) },
                { "databaseProvider", value => dict[ActivityPropertyName.DatabaseProvider] = MapDatabaseProvider(value.GetString()) },
                { "databaseManagementSystem", value => dict[ActivityPropertyName.DatabaseManagementSystem] = MapDbms(value.GetString()) },
                { "separateTenantSchema", value => dict[ActivityPropertyName.IsSeparateTenantSchema] = ParseBool(value) },
                { "theme", value => dict[ActivityPropertyName.Theme] = MapUiTheme(value.GetString()) },
                { "themeStyle", value => dict[ActivityPropertyName.ThemeStyle] = MapUiThemeStyle(value.GetString()) },
                { "mobileFramework", value => dict[ActivityPropertyName.MobileFramework] = MapMobileApp(value.GetString()) },
                { "publicWebsite", value => dict[ActivityPropertyName.HasPublicWebsite] = ParseBool(value) },
                { "includeTests", value => dict[ActivityPropertyName.IncludeTests] = ParseBool(value) },
                { "multiTenancy", value => dict[ActivityPropertyName.MultiTenancy] = ParseBool(value) },
                { "dynamicLocalization", value => dict[ActivityPropertyName.DynamicLocalization] = ParseBool(value) },
                { "kubernetesConfiguration", value => dict[ActivityPropertyName.KubernetesConfiguration] = ParseBool(value) },
                { "grafanaDashboard", value => dict[ActivityPropertyName.GrafanaDashboard] = ParseBool(value) },
                { "socialLogin", value => dict[ActivityPropertyName.SocialLogins] = ParseBool(value) }
            };
            foreach (var mapping in mappings)
            {
                try
                {
                    if (config.TryGetProperty(mapping.Key, out var propertyValue))
                    {
                        mapping.Value(propertyValue);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static bool ParseBool(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var b) => b,
            _ => false
        };


    protected virtual List<SolutionModuleInstallationInfo> ExtractModules(JsonElement modulesElement)
    {
        var modules = new List<SolutionModuleInstallationInfo>();

        foreach (var module in modulesElement.EnumerateObject())
        {
            var name = module.Name;
            if (!module.Value.TryGetProperty("path", out var pathElement))
            {
                continue;
            }

            var path = pathElement.GetString();
            if (path.IsNullOrEmpty() || !File.Exists(path))
            {
                continue;
            }

            var moduleJson = File.ReadAllText(path);
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

                if (modules.Any(x => x.ModuleName == name && x.Version == version))
                {
                    continue;
                }

                modules.Add(new SolutionModuleInstallationInfo
                {
                    ModuleName = name, Version = version, InstallationTime = creationTime
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

        using var httpClient = new HttpClient();
        var accessToken = File.ReadAllText(AbpTelemetryPaths.AccessToken);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var json = await httpClient.GetStringAsync($"{AbpPlatformUrls.AbpIoUrl}api/license/api-key");
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("licenseType").GetInt32();
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