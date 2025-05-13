using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Domain.Telemetry;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class DomainInfoContributor : ITelemetryApplicationInfoContributor , ISingletonDependency
{
    public Task ContributeAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue(ActivityPropertyName.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath);

            var entityCount = assembly.GetTypes().Count(t => typeof(IEntity).IsAssignableFrom(t) && !t.IsAbstract);

            activityData[ActivityPropertyName.EntityCount] =  entityCount;
        }

        return Task.CompletedTask;
    }
}