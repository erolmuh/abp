using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.EnvironmentInspection;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.Shared;
using Volo.Abp.Telemetry.Shared.Enums;
using DatabaseProvider = Volo.Abp.Telemetry.Shared.Enums.DatabaseProvider;
using MobileApp = Volo.Abp.Telemetry.Shared.Enums.MobileApp;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityDataProvider : IActivityDataProvider, IScopedDependency
{
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly ISoftwareInfoProvider _softwareInfoProvider;
    private readonly IEnumerable<ITelemetryApplicationInfoContributor> _applicationInfoContributors;

    public ActivityDataProvider(IDeviceInfoProvider deviceInfoProvider,
        ISoftwareInfoProvider softwareInfoProvider,
        IEnumerable<ITelemetryApplicationInfoContributor> applicationInfoContributors)
    {
        _deviceInfoProvider = deviceInfoProvider;
        _softwareInfoProvider = softwareInfoProvider;
        _applicationInfoContributors = applicationInfoContributors;
    }


    public async Task AddDeviceInformationAsync(ActivityData activityData)
    {
        activityData.Add(ActivityPropertyName.DeviceId, await _deviceInfoProvider.GetDeviceIdAsync());
        activityData.Add(ActivityPropertyName.DeviceType, _deviceInfoProvider.GetDeviceType());
        activityData.Add(ActivityPropertyName.DeviceLanguage, _deviceInfoProvider.GetLanguage());
        activityData.Add(ActivityPropertyName.OperatingSystem, _deviceInfoProvider.GetOperatingSystem());
        activityData.Add(ActivityPropertyName.CountryIsoCode, _deviceInfoProvider.GetCountry());

        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activityData.Add(ActivityPropertyName.InstalledSoftwares, softwareList);
    }

    public async Task AddApplicationInformation(ActivityData activityData)
    {
        foreach (var applicationInfoContributor in _applicationInfoContributors)
        {
            await applicationInfoContributor.ContributeAsync(activityData);
        }
    }

    public async Task AddSolutionInformationAsync(ActivityData activityData)
    {
        if (!activityData.TryGetValue(ActivityPropertyName.SolutionPath, out var value))
        {
            return;
        }

        var solutionPath = value as string;
        if (string.IsNullOrEmpty(solutionPath) || !File.Exists(solutionPath))
        {
            return;
        }

        var solutionJson = await File.ReadAllTextAsync(solutionPath);
        using var solutionDoc = JsonDocument.Parse(solutionJson);
        var root = solutionDoc.RootElement;

        if (!TryAddSolutionId(activityData, root, out var solutionId))
        {
            return;
        }

        if (root.TryGetProperty("creatingStudioConfiguration", out var config))
        {
            AddCreatingStudioConfiguration(activityData, config);
        }

        activityData.Add(ActivityPropertyName.LicenseType, await GetLicenseTypeAsync());

        if (root.TryGetProperty("modules", out var modulesElement) && modulesElement.ValueKind == JsonValueKind.Object)
        {
            var modules = await ExtractModulesAsync(modulesElement);
            activityData.Add(ActivityPropertyName.InstalledModules, modules);
        }
    }

    private bool TryAddSolutionId(ActivityData activityData, JsonElement root, out Guid solutionId)
    {
        solutionId = Guid.Empty;
        if (!root.TryGetProperty("id", out var idElement) ||
            !Guid.TryParse(idElement.GetString(), out solutionId))
        {
            return false;
        }

        activityData.Add(ActivityPropertyName.SolutionId, solutionId);
        return true;
    }

    private void AddCreatingStudioConfiguration(ActivityData activityData, JsonElement config)
    {
        activityData.Add(ActivityPropertyName.Template,
            MapSolutionTemplate(config.GetProperty("template").GetString()));
        activityData.Add(ActivityPropertyName.CreatedAbpStudioVersion,
            config.GetProperty("createdAbpStudioVersion").GetString()!);
        activityData.Add(ActivityPropertyName.IsTiered, config.GetProperty("Tiered").GetBoolean());
        activityData.Add(ActivityPropertyName.UiFramework,
            MapUiFramework(config.GetProperty("uiFramework").GetString()));
        activityData.Add(ActivityPropertyName.DatabaseProvider,
            MapDatabaseProvider(config.GetProperty("databaseProvider").GetString()));
        activityData.Add(ActivityPropertyName.DatabaseManagementSystem,
            MapDbms(config.GetProperty("databaseManagementSystem").GetString()));
        activityData.Add(ActivityPropertyName.IsSeparateTenantSchema,
            config.GetProperty("separateTenantSchema").GetBoolean());
        activityData.Add(ActivityPropertyName.Theme, MapUiTheme(config.GetProperty("theme").GetString()));
        activityData.Add(ActivityPropertyName.ThemeStyle,
            MapUiThemeStyle(config.GetProperty("themeStyle").GetString()));
        activityData.Add(ActivityPropertyName.MobileFramework,
            MapMobileApp(config.GetProperty("mobileFramework").GetString()));
        activityData.Add(ActivityPropertyName.HasPublicWebsite, config.GetProperty("publicWebsite").GetBoolean());
        activityData.Add(ActivityPropertyName.IncludeTests, config.GetProperty("includeTests").GetBoolean());
        activityData.Add(ActivityPropertyName.MultiTenancy, config.GetProperty("multiTenancy").GetBoolean());
        activityData.Add(ActivityPropertyName.DynamicLocalization,
            config.GetProperty("dynamicLocalization").GetBoolean());
        activityData.Add(ActivityPropertyName.KubernetesConfiguration,
            config.GetProperty("kubernetesConfiguration").GetBoolean());
        activityData.Add(ActivityPropertyName.GrafanaDashboard, config.GetProperty("grafanaDashboard").GetBoolean());
        activityData.Add(ActivityPropertyName.SocialLogins, config.GetProperty("socialLogin").GetBoolean());
    }

    private async Task<List<SolutionModuleInstallationInfo>> ExtractModulesAsync(JsonElement modulesElement)
    {
        var modules = new List<SolutionModuleInstallationInfo>();

        foreach (var moduleProperty in modulesElement.EnumerateObject())
        {
            var name = moduleProperty.Name;
            if (!moduleProperty.Value.TryGetProperty("path", out var pathElement))
            {
                continue;
            }

            var path = pathElement.GetString();
            if (path.IsNullOrEmpty() || !File.Exists(path))
            {
                continue;
            }

            var moduleJson = await File.ReadAllTextAsync(path);
            using var moduleDoc = JsonDocument.Parse(moduleJson);

            if (!moduleDoc.RootElement.TryGetProperty("imports", out var importsElement) ||
                importsElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var importProperty in importsElement.EnumerateObject())
            {
                var importValue = importProperty.Value;
                var version = importValue.GetProperty("version").GetString();
                var creationTime = importValue.TryGetProperty("creationTime", out var ct)
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


    private Dbms MapDbms(string? databaseManagementSystem)
    {
        return databaseManagementSystem switch
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

    private UiTheme MapUiTheme(string? theme)
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

    private MobileApp MapMobileApp(string? mobileFramework)
    {
        return mobileFramework switch
        {
            "maui" => MobileApp.Maui,
            "react-native" => MobileApp.ReactNative,
            "none" => MobileApp.None,
            _ => MobileApp.Unknown
        };
    }

    private UiThemeStyle MapUiThemeStyle(string? themeStyle)
    {
        return themeStyle switch
        {
            "dim" => UiThemeStyle.Dim,
            "style" => UiThemeStyle.System,
            "dark" => UiThemeStyle.Dark,
            "light" => UiThemeStyle.Light,
            _ => UiThemeStyle.Unknown
        };
    }

    private SolutionTemplate MapSolutionTemplate(string? templateName)
    {
        return templateName switch
        {
            "microservice" => SolutionTemplate.Microservice,
            "app-nolayers" => SolutionTemplate.AppNoLayers,
            "app" => SolutionTemplate.AppLayered,
            _ => SolutionTemplate.Unknown
        };
    }

    private UiFramework MapUiFramework(string? uiFramework)
    {
        return uiFramework switch
        {
            "mvc" => UiFramework.MvcRazorPages,
            "blazor" => UiFramework.BlazorWasm,
            "angular" => UiFramework.Angular,
            "blazor-server" => UiFramework.BlazorServer,
            "blazor-webapp" => UiFramework.BlazorWebApp,
            "maui-blazor" => UiFramework.BlazorMaUI,
            "none" => UiFramework.None,
            _ => UiFramework.Unknown,
        };
    }

    private DatabaseProvider MapDatabaseProvider(string? databaseProvider)
    {
        return databaseProvider switch
        {
            "ef" => DatabaseProvider.EfCore,
            "mongodb" => DatabaseProvider.MongoDb,
            "none" => DatabaseProvider.None,
            _ => DatabaseProvider.Unknown
        };
    }


    private async Task<int> GetLicenseTypeAsync()
    {
        if (!File.Exists(AbpTelemetryPaths.AccessToken))
        {
            return 0;
        }

        using var httpClient = new HttpClient();
        var accessToken = File.ReadAllText(AbpTelemetryPaths.AccessToken);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await using var stream = await httpClient.GetStreamAsync($"{AbpPlatformUrls.AbpIoUrl}api/license/api-key");
        using var doc = await JsonDocument.ParseAsync(stream);

        return doc.RootElement.GetProperty("licenseType").GetInt32();
    }
}