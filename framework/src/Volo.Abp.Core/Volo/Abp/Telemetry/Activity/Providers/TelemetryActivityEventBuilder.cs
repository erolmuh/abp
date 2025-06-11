using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class TelemetryActivityEventBuilder : ITelemetryActivityEventBuilder, ISingletonDependency //TODO: Transient?
{
    private readonly IServiceProvider _serviceProvider;

    public TelemetryActivityEventBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public virtual async Task BuildAsync(ActivityEvent activity)
    {
        var scope = _serviceProvider.CreateScope();
        var sessionProvider = scope.ServiceProvider.GetRequiredService<ITelemetrySessionProvider>();
        var activityDataEnrichers = scope.ServiceProvider.GetRequiredService<IEnumerable<ITelemetryActivityEventEnricher>>();
        
        await sessionProvider.AddSessionInfoAsync(activity);

        foreach (var activityDataEnricher in activityDataEnrichers)
        {
            await activityDataEnricher.EnrichAsync(activity);
        }
    }
}
