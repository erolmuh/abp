using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityDataEnricher))]
public class TelemetryApplicationInfoEnricher : ITelemetryActivityDataEnricher, ISingletonDependency
{
    private readonly ITelemetrySessionTypeProvider _telemetrySessionTypeProvider;
    private readonly IEnumerable<ITelemetryApplicationInfoContributor> _telemetryApplicationInfoContributors;

    public TelemetryApplicationInfoEnricher(ITelemetrySessionTypeProvider telemetrySessionTypeProvider,
        IEnumerable<ITelemetryApplicationInfoContributor> telemetryApplicationInfoContributors)
    {
        _telemetrySessionTypeProvider = telemetrySessionTypeProvider;
        _telemetryApplicationInfoContributors = telemetryApplicationInfoContributors;
    }

    public async Task EnrichAsync(ActivityData activity)
    {
        if (activity.ContainsKey(ActivityPropertyName.Assembly) && _telemetrySessionTypeProvider.SessionType == SessionType.ApplicationRuntime)
        {
            foreach (var contributor in _telemetryApplicationInfoContributors)
            {
                await contributor.ContributeAsync(activity);
            }

            activity.Remove(ActivityPropertyName.Assembly);
        }
    }
}