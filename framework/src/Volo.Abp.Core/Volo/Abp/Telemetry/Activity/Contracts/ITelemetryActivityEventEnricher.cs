using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityEventEnricher
{
    Task EnrichAsync(ActivityEvent activity);
}