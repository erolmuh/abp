namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public interface ISoftwareInfoProvider
{
    Task<List<SoftwareInfo>> GetSoftwareInfoAsync(CancellationToken cancellationToken = default);
}