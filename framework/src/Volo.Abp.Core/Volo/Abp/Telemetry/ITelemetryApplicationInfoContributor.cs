using System.Threading.Tasks;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public interface ITelemetryApplicationInfoContributor
{
    Task ContributeAsync(ActivityData activityData);
}