using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityDataEnricher
{
    Task EnrichAsync(ActivityData activity);
}