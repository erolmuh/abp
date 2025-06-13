using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Reflection;

namespace Volo.Abp.AspNetCore.Mvc;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class TelemetryApplicationMetricsContributor : ITelemetryApplicationInfoContributor, ISingletonDependency
{
    private readonly ITypeFinder _typeFinder;

    public TelemetryApplicationMetricsContributor(ITypeFinder typeFinder)
    {
        _typeFinder = typeFinder;
    }
    
    public Task ContributeAsync(ActivityEvent activityEvent)
    {
        // TODO: Exclude types with namespace "Volo."
        // TODO: Use ITypeFinder
        
        if (activityEvent.TryGetValue(ActivityPropertyNames.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath); //TODO: !!!!
            var types = assembly.GetTypes();

            var appServiceCount = types.Count(t =>
                typeof(IApplicationService).IsAssignableFrom(t) &&
                t is { IsAbstract: false, IsInterface: false });

            var controllerCount = types.Count(t =>
                typeof(ControllerBase).IsAssignableFrom(t) &&
                !t.IsAbstract);

            activityEvent[ActivityPropertyNames.AppServiceCount] = appServiceCount;
            activityEvent[ActivityPropertyNames.ControllerCount] = controllerCount;
        }
        
        return Task.CompletedTask;
    }
}