namespace Volo.Abp.Telemetry.Constants;

public static class AbpPlatformUrls
{
#if DEBUG
    public const string AbpTelemetryApiUrl = "https://localhost:44393/";
    public const string AbpIoUrl = "https://localhost:44328/";
#else
    public const string AbpTelemetryApiUrl = "https://telemetry.abp.io/";
    public const string AbpIoUrl = "https://abp.io/";

#endif


}