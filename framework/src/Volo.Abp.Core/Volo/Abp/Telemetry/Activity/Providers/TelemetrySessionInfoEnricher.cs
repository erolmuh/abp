using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;
[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetrySessionInfoEnricher : ITelemetryActivityEventEnricher , IScopedDependency
{
    public bool IsFirstRun => true;
    public Type? DependsOn => null;
    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(true);
    }

    public Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        var result = new Dictionary<string, object>
        {
            { ActivityPropertyNames.SessionType, SessionType.ApplicationRuntime },
            { ActivityPropertyNames.SessionId, Guid.NewGuid().ToString() },
            { ActivityPropertyNames.IsFirstSession, !File.Exists(TelemetryPaths.ActivityStorage) }
        };

        return Task.FromResult<Dictionary<string, object>?>(result);
    }
}