using System.Threading.Tasks;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public interface ITelemetrySessionProvider
{
    Task AddSessionInfoAsync(ActivityData activity);
}