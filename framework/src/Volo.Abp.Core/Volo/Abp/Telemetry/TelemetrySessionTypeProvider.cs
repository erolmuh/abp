using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry;

[ExposeServices(typeof(ITelemetrySessionTypeProvider))]
public class TelemetrySessionTypeProvider : ITelemetrySessionTypeProvider, ISingletonDependency
{
    public SessionType SessionType => SessionType.ApplicationRuntime;
}