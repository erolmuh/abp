namespace Volo.Abp.Telemetry;

public interface IApplicationScanner
{
    ApplicationInfo Scan();
}