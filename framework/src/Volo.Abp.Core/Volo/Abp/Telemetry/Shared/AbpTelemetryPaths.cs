using System;
using System.IO;

namespace Volo.Abp.Telemetry.Shared;

public static class AbpTelemetryPaths
{
    public static string AccessToken => Path.Combine(AbpRootPath, "cli", "access-token.bin");
    public static string ComputerId => Path.Combine(AbpRootPath, "cli", "computer-id.bin");
    public static string ActivityStorage => Path.Combine(AbpRootPath, "activity-storage.json");
    public static string Studio => Path.Combine(AbpRootPath, "studio");
    private readonly static string AbpRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".abp");
}



public static class AbpPlatformUrls
{
 
    
#if DEBUG
    public const string TelemetryApiUrl = "https://localhost:44393/";
    public const string AbpIoUrl = "https://localhost:44328/";
#else
    public const string TelemetryApiUrl = "https://telemetry.abp.io/";
    public const string AbpIoUrl = "https://abp.io/";

#endif


}