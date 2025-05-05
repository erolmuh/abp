using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnvironmentInspection;
using EnvironmentInspection.Contracts;
using EnvironmentInspection.Enums;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;
using DatabaseProvider = EnvironmentInspection.Enums.DatabaseProvider;
using MobileApp = EnvironmentInspection.Enums.MobileApp;

namespace Activity;

public class ActivityDataProvider : IActivityDataProvider, IScopedDependency
{
    private readonly IActivityStorage _activityStorage;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly ISoftwareInfoProvider _softwareInfoProvider;
    private readonly ApplicationInfoProvider _applicationInfoProvider;

    public ActivityDataProvider(IActivityStorage activityStorage, IDeviceInfoProvider deviceInfoProvider,
        ISoftwareInfoProvider softwareInfoProvider, ApplicationInfoProvider applicationInfoProvider)
    {
        _activityStorage = activityStorage;
        _deviceInfoProvider = deviceInfoProvider;
        _softwareInfoProvider = softwareInfoProvider;
        _applicationInfoProvider = applicationInfoProvider;
    }

    public virtual async Task<ActivityData> AddExtraInformationAsync(ActivityData activityData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (isFirstSession, sessionId) = await _activityStorage.GetOrCreateSessionInfoAsync(cancellationToken);

            activityData.Add("SessionId", sessionId);
            activityData.Add("IsFirstSession", isFirstSession);
            
            var lastDeviceInfoSendTime = await _activityStorage.GetLastDeviceInfoSendTimeAsync(cancellationToken);
            activityData.Add("DeviceId", await _deviceInfoProvider.GetDeviceIdAsync());

            if (lastDeviceInfoSendTime is null || DateTimeOffset.UtcNow - lastDeviceInfoSendTime > TimeSpan.FromDays(7))
            {
                await AddDeviceInformationAsync(activityData);
            }

            if (activityData.TryGetValue("ProjectAssemblyForScan", out var value))
            {
                var assembly = value as Assembly;
                if (assembly != null)
                {
                    AddApplicationInformation(activityData, assembly);
                }
            }

            await AddSolutionInformationAsync(activityData);

            return activityData;
        }
        catch
        {
            return activityData;
        }
    }

    private async Task AddDeviceInformationAsync(ActivityData activityData)
    {
        activityData.Add(ActivityPropertyNameConstants.DeviceType , _deviceInfoProvider.GetDeviceType());
        activityData.Add(ActivityPropertyNameConstants.DeviceLanguage, _deviceInfoProvider.GetLanguage());
        activityData.Add(ActivityPropertyNameConstants.OperatingSystem, _deviceInfoProvider.GetOperatingSystem());
        activityData.Add(ActivityPropertyNameConstants.CountryIsoCode, _deviceInfoProvider.GetCountry());

        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activityData.Add(ActivityPropertyNameConstants.InstalledSoftwares, softwareList);
        await _activityStorage.MarkDeviceInfoAsSentAsync();
    }

    private void AddApplicationInformation(ActivityData activityData, Assembly assembly)
    {
        var info = _applicationInfoProvider.Scan(assembly);

        activityData.Add(nameof(ApplicationInfo.ControllerCount), info.ControllerCount);
        activityData.Add(nameof(ApplicationInfo.EntityCount), info.EntityCount);
        activityData.Add(nameof(ApplicationInfo.AbpModuleCount), info.AbpModuleCount);
        activityData.Add(nameof(ApplicationInfo.PermissionCount), info.PermissionCount);
        activityData.Add(nameof(ApplicationInfo.AppServiceCount), info.AppServiceCount);
        activityData.Add(ActivityPropertyNameConstants.ProjectType, activityData["ProjectType"]);
        activityData.Add(ActivityPropertyNameConstants.ProjectId, activityData["ProjectId"]);

        activityData.Remove("ProjectAssemblyForScan");
    }

    private async Task AddSolutionInformationAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue("SolutionPath", out var value))
        {
            var solutionPath = value as string;

            var rootJObject = JObject.Parse(await File.ReadAllTextAsync(solutionPath!));
            var solutionId = rootJObject["id"]!.To<Guid>();
            var lastSolutionInfoSendTime =
                await _activityStorage.GetLastSolutionInfoSendTimeAsync(solutionId);
            activityData.Add(ActivityPropertyNameConstants.SolutionId, solutionId);

            if (lastSolutionInfoSendTime is null ||
                DateTimeOffset.UtcNow - lastSolutionInfoSendTime > TimeSpan.FromDays(7))
            {
                var infoJObject = (JObject)rootJObject["creatingStudioConfiguration"]!;
                activityData.Add(ActivityPropertyNameConstants.Template, GetSolutionTemplate(infoJObject["template"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.CreatedAbpStudioVersion, infoJObject["createdAbpStudioVersion"]!.ToString());
                activityData.Add(ActivityPropertyNameConstants.IsTiered, infoJObject.Value<bool>("Tiered"));
                activityData.Add(ActivityPropertyNameConstants.UiFramework, GetUiFramework(infoJObject["uiFramework"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.DatabaseProvider, GetDatabaseProvider(infoJObject["databaseProvider"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.DatabaseManagementSystem, GetDbms(infoJObject["databaseManagementSystem"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.IsSeparateTenantSchema, infoJObject.Value<bool>("separateTenantSchema"));
                activityData.Add(ActivityPropertyNameConstants.Theme, GetUiTheme(infoJObject["theme"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.ThemeStyle, GetUiThemeStyle(infoJObject["themeStyle"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.MobileFramework, GetMobileApp(infoJObject["mobileFramework"]?.ToString()));
                activityData.Add(ActivityPropertyNameConstants.HasPublicWebsite, infoJObject.Value<bool>("publicWebsite"));
                activityData.Add(ActivityPropertyNameConstants.IncludeTests, infoJObject.Value<bool>("includeTests"));
                activityData.Add(ActivityPropertyNameConstants.MultiTenancy, infoJObject.Value<bool>("multiTenancy"));
                activityData.Add(ActivityPropertyNameConstants.DynamicLocalization, infoJObject.Value<bool>("dynamicLocalization"));
                activityData.Add(ActivityPropertyNameConstants.KubernetesConfiguration, infoJObject.Value<bool>("kubernetesConfiguration"));
                activityData.Add(ActivityPropertyNameConstants.GrafanaDashboard, infoJObject.Value<bool>("grafanaDashboard"));
                activityData.Add(ActivityPropertyNameConstants.SocialLogins, infoJObject.Value<bool>("socialLogin"));

                var modules = new List<SolutionModuleInstallationInfo>();
                foreach (var module in rootJObject["modules"]!.Children<JProperty>())
                {
                    var name = module.Name;
                    var path = (string?)module.Value["path"];
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var moduleJson = await File.ReadAllTextAsync(path);
                    var moduleObj = JObject.Parse(moduleJson);
                    var imports = moduleObj["imports"] as JObject;

                    if (imports == null)
                    {
                        continue;
                    }

                    foreach (var import in imports.Properties())
                    {
                        var importValue = (JObject)import.Value;
                        var version = (string)importValue["version"]!;
                        var creationTime = importValue["creationTime"] != null
                            ? (DateTimeOffset?)DateTimeOffset.Parse((string)importValue["creationTime"]!)
                            : null;

                        if (modules.Any(x => x.ModuleName == name && x.Version == version))
                        {
                            continue;
                        }

                        modules.Add(new SolutionModuleInstallationInfo
                        {
                            ModuleName = name, Version = version, InstallationTime = creationTime
                        });
                    }

                    activityData.Add(ActivityPropertyNameConstants.InstalledModules, modules);
                }
            }
        }
    }

    private Dbms GetDbms(string? databaseManagementSystem)
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

    private UiTheme GetUiTheme(string? theme)
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

    private MobileApp GetMobileApp(string? mobileFramework)
    {
        return mobileFramework switch
        {
            "maui" => MobileApp.Maui,
            "react-native" => MobileApp.ReactNative,
            "none" => MobileApp.None,
            _ => MobileApp.Unknown
        };
    }

    private UiThemeStyle GetUiThemeStyle(string? themeStyle)
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

    private SolutionTemplate GetSolutionTemplate(string? templateName)
    {
        return templateName switch
        {
            "microservice" => SolutionTemplate.Microservice,
            "app-nolayers" => SolutionTemplate.AppNoLayers,
            "app" => SolutionTemplate.AppLayered,
            _ => SolutionTemplate.Unknown
        };
    }

    private UiFramework GetUiFramework(string? uiFramework)
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

    private DatabaseProvider GetDatabaseProvider(string? databaseProvider)
    {
        return databaseProvider switch
        {
            "ef" => DatabaseProvider.EfCore,
            "mongodb" => DatabaseProvider.MongoDb,
            "none" => DatabaseProvider.None,
            _ => DatabaseProvider.Unknown
        };
    }
}