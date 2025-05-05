using System;
using System.Collections.Generic;

namespace Activity;

public class ActivityData : Dictionary<string, object>
{
    public string ActivityName
    {
        get => (string) this[nameof(ActivityName)];
        init => this[nameof(ActivityName)] = value;
    }

    public string? ActivityDetails
    {
        get => (string?) this[nameof(ActivityDetails)];
        internal set
        {
            if (value is not null) this[nameof(ActivityDetails)] = value;
        }
    }

    public long? ActivityDuration
    {
        get => (long?) this[nameof(ActivityDuration)];
        internal set
        {
            if (value is not null)
            {
                this[nameof(ActivityDuration)] = value;
            }
        }
    }

    public DateTimeOffset Time = DateTimeOffset.UtcNow;

    public ActivityData(string activityName, string? details = null)
    {
        if (activityName.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(activityName));
        }

        ActivityName = activityName;
        ActivityDetails = details;
    }
}


public static class ActivityPropertyNameConstants
{
    public const string SessionId = "SessionId";
    public const string IsFirstSession = "IsFirstSession";
    public const string DeviceId = "DeviceId";
    public const string DeviceType = "DeviceType";
    public const string DeviceLanguage = "DeviceLanguage";
    public const string OperatingSystem = "OperatingSystem";
    public const string CountryIsoCode = "CountryIsoCode";
    public const string InstalledSoftwares = "InstalledSoftwares";
    public const string ControllerCount = nameof(ApplicationInfo.ControllerCount);
    public const string EntityCount = nameof(ApplicationInfo.EntityCount);
    public const string AbpModuleCount = nameof(ApplicationInfo.AbpModuleCount);
    public const string PermissionCount = nameof(ApplicationInfo.PermissionCount);
    public const string AppServiceCount = nameof(ApplicationInfo.AppServiceCount);
    public const string ProjectType = "ProjectType";
    public const string ProjectId = "ProjectId";
    public const string SolutionId = "SolutionId";
    public const string Template = "Template";
    public const string CreatedAbpStudioVersion = "CreatedAbpStudioVersion";
    public const string IsTiered = "IsTiered";
    public const string UiFramework = "UiFramework";
    public const string DatabaseProvider = "DatabaseProvider";
    public const string DatabaseManagementSystem = "DatabaseManagementSystem";
    public const string IsSeparateTenantSchema = "IsSeparateTenantSchema";
    public const string Theme = "Theme";
    public const string ThemeStyle = "ThemeStyle";
    public const string MobileFramework = "MobileFramework";
    public const string HasPublicWebsite = "HasPublicWebsite";
    public const string IncludeTests = "IncludeTests";
    public const string MultiTenancy = "MultiTenancy";
    public const string DynamicLocalization = "DynamicLocalization";
    public const string KubernetesConfiguration = "KubernetesConfiguration";
    public const string GrafanaDashboard = "GrafanaDashboard";
    public const string SocialLogins = "SocialLogins";
    public const string InstalledModules = "InstalledModules";
    public const string ProjectAssemblyForScan = "ProjectAssemblyForScan";
    public const string SolutionPath = "SolutionPath";
}