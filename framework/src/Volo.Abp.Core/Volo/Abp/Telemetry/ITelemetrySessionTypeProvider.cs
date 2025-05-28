using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry;

public interface ITelemetrySessionTypeProvider
{
    SessionType SessionType { get; }
}