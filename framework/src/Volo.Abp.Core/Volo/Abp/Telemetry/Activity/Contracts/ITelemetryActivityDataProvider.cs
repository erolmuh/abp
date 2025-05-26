using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityDataProvider
{
    Task AddExtraInformationAsync(ActivityData activity);
}