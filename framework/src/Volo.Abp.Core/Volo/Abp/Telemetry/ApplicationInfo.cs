namespace Volo.Abp.Telemetry;

public class ApplicationInfo
{
    public int EntityCount { get; set; }
    public int AppServiceCount { get; set; }
    public int ControllerCount { get; set; }
    public int PermissionCount { get; set; }
    public int AbpModuleCount { get; set; }
    public int ProjectCount { get; set; } 
}