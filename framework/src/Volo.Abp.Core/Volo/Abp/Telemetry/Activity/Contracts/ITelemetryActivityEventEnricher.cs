using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityEventEnricher
{
    bool IsFirstRun { get; } //TODO: ExecutionOrder
    Type? DependsOn { get; } 
    Task<bool> CanExecuteAsync(ActivityContext context);
    Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context);
    
}