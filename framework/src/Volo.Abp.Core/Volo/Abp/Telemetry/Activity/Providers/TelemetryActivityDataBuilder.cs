using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class TelemetryActivityDataBuilder : ITelemetryActivityDataBuilder, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;

    public TelemetryActivityDataBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public virtual async Task BuildAsync(ActivityData activity)
    {
        var telemetrySessionTypeProvider = _serviceProvider.GetRequiredService<ITelemetrySessionTypeProvider>();
        var telemetryActivityStorage = _serviceProvider.GetRequiredService<ITelemetryActivityStorage>();
        
        activity[ActivityPropertyNames.SessionType] = telemetrySessionTypeProvider.SessionType;
        activity[ActivityPropertyNames.SessionId] = await telemetryActivityStorage.GetOrCreateSessionInfoAsync();
        activity[ActivityPropertyNames.IsFirstSession] = !File.Exists(TelemetryPaths.ActivityStorage);
        
        var activityDataEnrichers = _serviceProvider.GetRequiredService<IEnumerable<ITelemetryActivityDataEnricher>>();

        foreach (var activityDataEnricher in activityDataEnrichers)
        {
            await activityDataEnricher.EnrichAsync(activity);
        }
    }
}