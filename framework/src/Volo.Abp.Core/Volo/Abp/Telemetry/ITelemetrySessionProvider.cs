using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public interface ITelemetrySessionProvider
{
    void AddSessionInfo(ActivityData activity);
}