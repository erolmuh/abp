using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class TelemetryActivityEventBuilder : ITelemetryActivityEventBuilder, IScopedDependency
{
    private readonly List<ITelemetryActivityEventEnricher> _activityEnrichers;

    public TelemetryActivityEventBuilder(IEnumerable<ITelemetryActivityEventEnricher> activityDataEnrichers)
    {
        _activityEnrichers = activityDataEnrichers
            .Where(x => x.GetType().Assembly.FullName!.StartsWith(TelemetryConsts.VoloNameSpaceFilter) && 
                        x is not IHasParentTelemetryActivityEventEnricher)
            .OrderByDescending(x => x.ExecutionOrder)
            .ToList();
    }

    public virtual async Task<ActivityEvent?> BuildAsync(ActivityContext context)
    {
        foreach (var enricher in _activityEnrichers)
        {
            try
            {
                if (context.IsTerminated)
                {
                    return null;
                }

                await enricher.EnrichAsync(context);
            }
            catch
            {
               //ignored
            }
        }

        return context.Current;
    }
}