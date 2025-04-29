using System.Reflection;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.EnvironmentInspection;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using DatabaseProvider = Volo.Abp.Telemetry.EnvironmentInspection.DatabaseProvider;
using MobileApp = Volo.Abp.Telemetry.EnvironmentInspection.MobileApp;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityDataProvider : IActivityDataProvider
{
    private IActivityStorage _activityStorage;
    public IAbpLazyServiceProvider ServiceProvider { protected get; set; }

    public virtual async Task<ActivityData> AddExtraInformationAsync(ActivityData activityData,
        CancellationToken cancellationToken = default)
    {
        _activityStorage = ServiceProvider.LazyGetRequiredService<IActivityStorage>();
        try
        {

            var (isFirstSession, sessionId) = await _activityStorage.GetOrCreateSessionInfoAsync(cancellationToken);
            
            activityData.Add("SessionId", sessionId);
            activityData.Add("IsFirstSession", isFirstSession);
            await AddDeviceInformationAsync(activityData);

            AddApplicationInformation(activityData);

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
        var deviceInfoProvider = ServiceProvider.LazyGetRequiredService<IDeviceInfoProvider>();
        var softwareInfoProvider = ServiceProvider.LazyGetRequiredService<ISoftwareInfoProvider>();
        
        var lastDeviceInfoSendTime = await _activityStorage.GetLastDeviceInfoSendTimeAsync();
        activityData.Add("DeviceId", await deviceInfoProvider.GetDeviceIdAsync());

        if (lastDeviceInfoSendTime is null || DateTimeOffset.UtcNow - lastDeviceInfoSendTime > TimeSpan.FromDays(7))
        {
            activityData.Add("DeviceType", deviceInfoProvider.GetDeviceType());
            activityData.Add("DeviceLanguage", deviceInfoProvider.GetLanguage());
            activityData.Add("OperatingSystem", deviceInfoProvider.GetOperatingSystem());
            activityData.Add("Country", deviceInfoProvider.GetCountry());

            var softwareList = await softwareInfoProvider.GetSoftwareInfoAsync();
            activityData.Add("InstalledSoftwares" , softwareList);
            await _activityStorage.MarkDeviceInfoAsSentAsync();
        }
    }

    private void AddApplicationInformation(ActivityData activityData)
    {
        if (activityData.ContainsKey("ProjectAssemblyForScan"))
        {
            var assembly = activityData["ProjectAssemblyForScan"] as Assembly;
            if (assembly != null)
            {
                var applicationInfoProvider = ServiceProvider.LazyGetRequiredService<ApplicationInfoProvider>();
                var info = applicationInfoProvider.Scan();

                activityData.Add(nameof(Telemetry.ApplicationInfo.ControllerCount), info.ControllerCount);
                activityData.Add(nameof(Telemetry.ApplicationInfo.EntityCount), info.EntityCount);
                activityData.Add(nameof(Telemetry.ApplicationInfo.AbpModuleCount), info.AbpModuleCount);
                activityData.Add(nameof(Telemetry.ApplicationInfo.PermissionCount), info.PermissionCount);
                activityData.Add(nameof(Telemetry.ApplicationInfo.AppServiceCount), info.AppServiceCount);
                activityData.Add("ProjectType", activityData["ProjectType"]);
                activityData.Add("ProjectId", activityData["ProjectId"]);

                activityData.Remove("ProjectAssemblyForScan");
            }
        }
    }

    private async Task AddSolutionInformationAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue("SolutionPath", out var value))
        {
            var solutionPath = value as string;

            var rootJObject = JObject.Parse(await File.ReadAllTextAsync(solutionPath));
            var solutionId = rootJObject["id"]!.To<Guid>();
            var lastSolutionInfoSendTime =
                await _activityStorage.GetLastSolutionInfoSendTimeAsync(solutionId);
            activityData.Add("SolutionId", solutionId);

            if (lastSolutionInfoSendTime is null ||
                DateTimeOffset.UtcNow - lastSolutionInfoSendTime > TimeSpan.FromDays(7))
            {
                var infoJObject = (JObject) rootJObject["creatingStudioConfiguration"]!;
                activityData.Add("Template", GetSolutionTemplate(infoJObject["template"]?.ToString()));
                activityData.Add("CreatedAbpStudioVersion", infoJObject["createdAbpStudioVersion"]?.ToString());
                activityData.Add("IsTiered", infoJObject.Value<bool>("Tiered"));
                activityData.Add("UiFramework", GetUiFramework(infoJObject["uiFramework"]?.ToString()));
                activityData.Add("DatabaseProvider", GetDatabaseProvider(infoJObject["databaseProvider"]?.ToString()));
                activityData.Add("DatabaseManagementSystem", GetDbms(infoJObject["databaseManagementSystem"]?.ToString()));
                activityData.Add("IsSeparateTenantSchema", infoJObject.Value<bool>("separateTenantSchema"));
                activityData.Add("Theme", GetUiTheme(infoJObject["theme"]?.ToString()));
                activityData.Add("ThemeStyle", GetUiThemeStyle(infoJObject["themeStyle"]?.ToString()));
                activityData.Add("MobileFramework", GetMobileApp(infoJObject["mobileFramework"]?.ToString()));
                activityData.Add("HasPublicWebsite", infoJObject.Value<bool>("publicWebsite"));
                activityData.Add("IncludeTests", infoJObject.Value<bool>("includeTests"));
                activityData.Add("MultiTenancy", infoJObject.Value<bool>("multiTenancy"));
                activityData.Add("DynamicLocalization", infoJObject.Value<bool>("dynamicLocalization"));
                activityData.Add("KubernetesConfiguration", infoJObject.Value<bool>("kubernetesConfiguration"));
                activityData.Add("GrafanaDashboard", infoJObject.Value<bool>("grafanaDashboard"));
                activityData.Add("SocialLogins", infoJObject.Value<bool>("socialLogin"));
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