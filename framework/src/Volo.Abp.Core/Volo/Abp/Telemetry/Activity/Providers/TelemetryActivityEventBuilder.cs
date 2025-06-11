using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class TelemetryActivityEventBuilder : ITelemetryActivityEventBuilder, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;

    public TelemetryActivityEventBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public virtual async Task BuildAsync(ActivityEvent activity)
    {
       
        var sessionProvider = _serviceProvider.GetRequiredService<ITelemetrySessionProvider>();
        
        await sessionProvider.AddSessionInfoAsync(activity);
        
        var activityDataEnrichers = _serviceProvider.GetRequiredService<IEnumerable<ITelemetryActivityEventEnricher>>();

        foreach (var activityDataEnricher in activityDataEnrichers)
        {
            await activityDataEnricher.EnrichAsync(activity);
        }
    }
}
