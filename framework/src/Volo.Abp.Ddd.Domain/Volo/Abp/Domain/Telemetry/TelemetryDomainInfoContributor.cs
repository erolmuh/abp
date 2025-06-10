using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Domain.Telemetry;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class TelemetryDomainInfoContributor : ITelemetryApplicationInfoContributor , ISingletonDependency
{
    public Task ContributeAsync(ActivityEvent activityEvent)
    {
        if (activityEvent.TryGetValue(ActivityPropertyNames.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath);

            var entityCount = assembly.GetTypes().Count(t => typeof(IEntity).IsAssignableFrom(t) && !t.IsAbstract);

            activityEvent[ActivityPropertyNames.EntityCount] =  entityCount;
        }

        return Task.CompletedTask;
    }
}