using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.EnvironmentInspection;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.EnvironmentInspection.Enums;
using DatabaseProvider = Volo.Abp.Telemetry.EnvironmentInspection.Enums.DatabaseProvider;
using MobileApp = Volo.Abp.Telemetry.EnvironmentInspection.Enums.MobileApp;

namespace Volo.Abp.Telemetry.Activity;

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

            activityData.Add(ActivityPropertyNameConstants.SessionId, sessionId);
            activityData.Add(ActivityPropertyNameConstants.IsFirstSession, isFirstSession);

            var lastDeviceInfoSendTime = await _activityStorage.GetLastDeviceInfoSendTimeAsync(cancellationToken);
            activityData.Add(ActivityPropertyNameConstants.DeviceId, await _deviceInfoProvider.GetDeviceIdAsync());

            if (lastDeviceInfoSendTime is null || DateTimeOffset.UtcNow - lastDeviceInfoSendTime > TimeSpan.FromDays(7))
            {
                await AddDeviceInformationAsync(activityData);
            }

            if (activityData.TryGetValue(ActivityPropertyNameConstants.Assembly, out var value))
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
        activityData.Add(ActivityPropertyNameConstants.DeviceType, _deviceInfoProvider.GetDeviceType());
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

        activityData.Add(ActivityPropertyNameConstants.ControllerCount, info.ControllerCount);
        activityData.Add(ActivityPropertyNameConstants.EntityCount, info.EntityCount);
        activityData.Add(ActivityPropertyNameConstants.AbpModuleCount, info.AbpModuleCount);
        activityData.Add(ActivityPropertyNameConstants.PermissionCount, info.PermissionCount);
        activityData.Add(ActivityPropertyNameConstants.AppServiceCount, info.AppServiceCount);
        activityData.Add(ActivityPropertyNameConstants.ProjectId, activityData[ActivityPropertyNameConstants.ProjectId]);

        activityData.Remove(ActivityPropertyNameConstants.Assembly);
    }

    private async Task AddSolutionInformationAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue(ActivityPropertyNameConstants.SolutionPath, out var value))
        {
            var solutionPath = value as string;
            if (string.IsNullOrEmpty(solutionPath) || !File.Exists(solutionPath))
            {
                return;
            }

            var solutionJson = File.ReadAllText(solutionPath);
            using var solutionDoc = JsonDocument.Parse(solutionJson);

            var root = solutionDoc.RootElement;

            if (!root.TryGetProperty("id", out var idElement) ||
                !Guid.TryParse(idElement.GetString(), out var solutionId))
            {
                return;
            }

            activityData.Add(ActivityPropertyNameConstants.SolutionId, solutionId);

            var lastSolutionInfoSendTime = await _activityStorage.GetLastSolutionInfoSendTimeAsync(solutionId);

            if (lastSolutionInfoSendTime is null ||
                DateTimeOffset.UtcNow - lastSolutionInfoSendTime > TimeSpan.FromDays(7))
            {
                if (root.TryGetProperty("creatingStudioConfiguration", out var config))
                {
                    activityData.Add(ActivityPropertyNameConstants.Template,
                        MapSolutionTemplate(config.GetProperty("template").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.CreatedAbpStudioVersion,
                        config.GetProperty("createdAbpStudioVersion").GetString()!);
                    activityData.Add(ActivityPropertyNameConstants.IsTiered, config.GetProperty("Tiered").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.UiFramework,
                        MapUiFramework(config.GetProperty("uiFramework").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.DatabaseProvider,
                        MapDatabaseProvider(config.GetProperty("databaseProvider").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.DatabaseManagementSystem,
                        MapDbms(config.GetProperty("databaseManagementSystem").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.IsSeparateTenantSchema,
                        config.GetProperty("separateTenantSchema").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.Theme,
                        MapUiTheme(config.GetProperty("theme").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.ThemeStyle,
                        MapUiThemeStyle(config.GetProperty("themeStyle").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.MobileFramework,
                        MapMobileApp(config.GetProperty("mobileFramework").GetString()));
                    activityData.Add(ActivityPropertyNameConstants.HasPublicWebsite,
                        config.GetProperty("publicWebsite").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.IncludeTests,
                        config.GetProperty("includeTests").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.MultiTenancy,
                        config.GetProperty("multiTenancy").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.DynamicLocalization,
                        config.GetProperty("dynamicLocalization").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.KubernetesConfiguration,
                        config.GetProperty("kubernetesConfiguration").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.GrafanaDashboard,
                        config.GetProperty("grafanaDashboard").GetBoolean());
                    activityData.Add(ActivityPropertyNameConstants.SocialLogins,
                        config.GetProperty("socialLogin").GetBoolean());
                }

                var modules = new List<SolutionModuleInstallationInfo>();

                if (root.TryGetProperty("modules", out var modulesElement) &&
                    modulesElement.ValueKind == JsonValueKind.Object)
                {
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

                        var moduleJson = File.ReadAllText(path);
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

                    activityData.Add(ActivityPropertyNameConstants.InstalledModules, modules);
                }
            }
        }
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
}