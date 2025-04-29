using Volo.Abp.DependencyInjection;

namespace Volo.Abp.Telemetry;

public interface ITelemetryDataSender : IScopedDependency
{
    Task SendAsync();
}