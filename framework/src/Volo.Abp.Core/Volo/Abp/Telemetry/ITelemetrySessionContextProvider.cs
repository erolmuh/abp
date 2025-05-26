using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry;

public interface ITelemetrySessionContextProvider
{
    public SessionType SessionType { get; }
    void SetSolutionContext(ActivityData activity);
}