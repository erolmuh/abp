using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityEventBuilder
{
    Task BuildAsync(ActivityEvent activity);
}