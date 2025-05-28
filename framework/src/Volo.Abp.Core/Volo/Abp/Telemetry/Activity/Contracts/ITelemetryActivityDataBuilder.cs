using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityDataBuilder
{
    Task BuildAsync(ActivityData activity);
}