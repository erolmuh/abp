using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
    private readonly IActivityStorage _activityStorage;

    public ActivityDataProvider(
        IDeviceInfoProvider deviceInfoProvider,
        ISoftwareInfoProvider softwareInfoProvider,
        IEnumerable<ITelemetryApplicationInfoContributor> applicationInfoContributors,
        IActivityStorage activityStorage)
    {
        _deviceInfoProvider = deviceInfoProvider;
        _softwareInfoProvider = softwareInfoProvider;
        _applicationInfoContributors = applicationInfoContributors;
        _activityStorage = activityStorage;
    }

    public virtual SessionType GetSessionType()
    {
        return SessionType.ApplicationRuntime;
    }

    public virtual async Task AddExtraInformationAsync(ActivityData activity)
    {
        var (isFirstSession, sessionId) = await _activityStorage.GetOrCreateSessionInfoAsync();

        AddSolutionId(activity);
        var sessionType = GetSessionType();
        activity[ActivityPropertyName.SessionType] = sessionType;
        activity[ActivityPropertyName.SessionId] = sessionId;
        activity[ActivityPropertyName.IsFirstSession] = isFirstSession;
        activity[ActivityPropertyName.DeviceId] = _deviceInfoProvider.GetDeviceId();

        if (await ShouldAddDeviceInfoAsync())
        {
            await AddDeviceInformationAsync(activity);
        }

        if (activity.ContainsKey(ActivityPropertyName.Assembly) && sessionType == SessionType.ApplicationRuntime)
        {
            await AddApplicationInformationAsync(activity);
        }

        if (await ShouldAddSolutionInformation(activity))
        {
            await AddSolutionInformationAsync(activity);
        }
    }

    protected virtual async Task<bool> ShouldAddDeviceInfoAsync()
    {
        var lastSend = await _activityStorage.GetLastDeviceInfoSendTimeAsync();
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > TimeSpan.FromDays(7);
    }

    protected virtual async Task<bool> ShouldAddSolutionInformation(ActivityData activity)
    {

        if (
            !activity.TryGetValue(ActivityPropertyName.SolutionId, out var id) ||
            !Guid.TryParse(id.ToString(), out var solutionId))
        {
            return false;
        }

        var lastSend = await _activityStorage.GetLastSolutionInfoSendTimeAsync(solutionId);
        return lastSend is null || DateTimeOffset.UtcNow - lastSend > TimeSpan.FromDays(7);
    }

    protected virtual async Task AddDeviceInformationAsync(ActivityData activityData)
    {
        activityData[ActivityPropertyName.DeviceType] = _deviceInfoProvider.GetDeviceType();
        activityData[ActivityPropertyName.DeviceLanguage] = _deviceInfoProvider.GetLanguage();
        activityData[ActivityPropertyName.OperatingSystem] = _deviceInfoProvider.GetOperatingSystem();
        activityData[ActivityPropertyName.CountryIsoCode] = _deviceInfoProvider.GetCountry();

        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activityData[ActivityPropertyName.InstalledSoftwares] = softwareList;
    }

    protected virtual async Task AddApplicationInformationAsync(ActivityData activityData)
    {
        foreach (var contributor in _applicationInfoContributors)
        {
            await contributor.ContributeAsync(activityData);
        }
    }

    protected virtual async Task AddSolutionInformationAsync(ActivityData activityData)
    {
        var solutionJson = File.ReadAllText(activityData[ActivityPropertyName.SolutionPath].ToString()!);
        using var solutionDoc = JsonDocument.Parse(solutionJson);
        var root = solutionDoc.RootElement;


        if (root.TryGetProperty("creatingStudioConfiguration", out var config))
        {
            AddCreatingStudioConfiguration(activityData, config);
        }

        activityData[ActivityPropertyName.LicenseType] =  await GetLicenseTypeAsync();

        if (root.TryGetProperty("modules", out var modulesElement) && modulesElement.ValueKind == JsonValueKind.Object)
        {
            var modules = ExtractModules(modulesElement);
            activityData[ActivityPropertyName.InstalledModules] = modules;
        }
    }

    protected virtual void AddSolutionId(ActivityData activityData)
    {
        if (activityData.ContainsKey(ActivityPropertyName.SolutionId))
        {
            return;
        }

        if (!activityData.TryGetValue(ActivityPropertyName.SolutionPath, out var path) || !File.Exists((string)path))
        {
            return;
        }

        var solutionJson = File.ReadAllText((string)path);
        using var solutionDoc = JsonDocument.Parse(solutionJson);
        var root = solutionDoc.RootElement;

        var solutionId = ReadSolutionIdFromJson(root);
        activityData[ActivityPropertyName.SolutionId] = solutionId;
    }

    protected virtual Guid ReadSolutionIdFromJson(JsonElement root)
    {
        return root.TryGetProperty("id", out var idElement) && Guid.TryParse(idElement.GetString(), out var id)
            ? id
            : throw new Exception("Solution ID is not valid.");
    }

    protected virtual void AddCreatingStudioConfiguration(ActivityData activityData, JsonElement config)
    {
        activityData[ActivityPropertyName.Template] = MapSolutionTemplate(config.GetProperty("template").GetString());
        activityData[ActivityPropertyName.CreatedAbpStudioVersion] = config.GetProperty("createdAbpStudioVersion").GetString()!;
        activityData[ActivityPropertyName.IsTiered] = config.GetProperty("Tiered").GetBoolean();
        activityData[ActivityPropertyName.UiFramework] = MapUiFramework(config.GetProperty("uiFramework").GetString());
        activityData[ActivityPropertyName.DatabaseProvider] = MapDatabaseProvider(config.GetProperty("databaseProvider").GetString());
        activityData[ActivityPropertyName.DatabaseManagementSystem] = MapDbms(config.GetProperty("databaseManagementSystem").GetString());
        activityData[ActivityPropertyName.IsSeparateTenantSchema] = config.GetProperty("separateTenantSchema").GetBoolean();
        activityData[ActivityPropertyName.Theme] = MapUiTheme(config.GetProperty("theme").GetString());
        activityData[ActivityPropertyName.ThemeStyle] = MapUiThemeStyle(config.GetProperty("themeStyle").GetString());
        activityData[ActivityPropertyName.MobileFramework] = MapMobileApp(config.GetProperty("mobileFramework").GetString());
        activityData[ActivityPropertyName.HasPublicWebsite] = config.GetProperty("publicWebsite").GetBoolean();
        activityData[ActivityPropertyName.IncludeTests] = config.GetProperty("includeTests").GetBoolean();
        activityData[ActivityPropertyName.MultiTenancy] = config.GetProperty("multiTenancy").GetBoolean();
        activityData[ActivityPropertyName.DynamicLocalization] = config.GetProperty("dynamicLocalization").GetBoolean();
        activityData[ActivityPropertyName.KubernetesConfiguration] = config.GetProperty("kubernetesConfiguration").GetBoolean();
        activityData[ActivityPropertyName.GrafanaDashboard] = config.GetProperty("grafanaDashboard").GetBoolean();
        activityData[ActivityPropertyName.SocialLogins] = config.GetProperty("socialLogin").GetBoolean();

    }

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

            if (!moduleDoc.RootElement.TryGetProperty("imports", out var imports) || imports.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var import in imports.EnumerateObject())
            {
                var version = import.Value.GetProperty("version").GetString();
                var creationTime = import.Value.TryGetProperty("creationTime", out var ct) ? DateTimeOffset.Parse(ct.GetString()!) : (DateTimeOffset?)null;

                if (modules.Any(x => x.ModuleName == name && x.Version == version))
                {
                    continue;
                }

                modules.Add(new SolutionModuleInstallationInfo { ModuleName = name, Version = version, InstallationTime = creationTime });
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
