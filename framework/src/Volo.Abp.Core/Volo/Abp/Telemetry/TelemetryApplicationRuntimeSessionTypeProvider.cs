using System.IO;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry;

public class TelemetryApplicationRuntimeSessionTypeProvider : ITelemetrySessionContextProvider , ISingletonDependency
{
    private readonly ITelemetrySolutionInfoProvider _telemetrySolutionInfoProvider;

    public TelemetryApplicationRuntimeSessionTypeProvider(ITelemetrySolutionInfoProvider telemetrySolutionInfoProvider)
    {
        _telemetrySolutionInfoProvider = telemetrySolutionInfoProvider;
    }

    public virtual SessionType SessionType => SessionType.ApplicationRuntime;
    public virtual void SetSolutionContext(ActivityData activity)
    {
        if (activity.ContainsKey(ActivityPropertyName.SolutionId))
        {
            return;
        }

        if (!activity.TryGetValue(ActivityPropertyName.SolutionPath, out var path) || !File.Exists((string)path))
        {
            return;
        }

        var solutionId = _telemetrySolutionInfoProvider.GetSolutionId((string)path);
        
        if (solutionId.HasValue)
        {
            activity[ActivityPropertyName.SolutionId] = solutionId.Value;
        }
    }
}