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
        var (sessionId, isFirstSession) = await telemetryActivityStorage.GetOrCreateSessionAsync();
        activity[ActivityPropertyNames.SessionType] = telemetrySessionTypeProvider.SessionType;
        activity[ActivityPropertyNames.SessionId] = sessionId;
        activity[ActivityPropertyNames.IsFirstSession] = isFirstSession;
        
        var activityDataEnrichers = _serviceProvider.GetRequiredService<IEnumerable<ITelemetryActivityDataEnricher>>();

        foreach (var activityDataEnricher in activityDataEnrichers)
        {
            await activityDataEnricher.EnrichAsync(activity);
        }
    }
}