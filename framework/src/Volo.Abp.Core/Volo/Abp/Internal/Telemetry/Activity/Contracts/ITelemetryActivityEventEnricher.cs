using System.Threading.Tasks;

namespace Volo.Abp.Internal.Telemetry.Activity.Contracts;

public interface ITelemetryActivityEventEnricher
{
    int ExecutionOrder { get; } 
    Task<bool> CanExecuteAsync(ActivityContext context);
    Task EnrichAsync(ActivityContext context);
    
}