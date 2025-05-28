using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class TelemetryModuleInfoContributor : ITelemetryApplicationInfoContributor, ISingletonDependency
{
    public Task ContributeAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue(ActivityPropertyNames.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath);
            var types = assembly.GetTypes();
            var moduleCount = types.Count(t => typeof(AbpModule).IsAssignableFrom(t) && !t.IsAbstract);
            var projectCount = types.Select(t => t.Assembly.GetName().Name).Distinct().Count();

            activityData[ActivityPropertyNames.ModuleCount] =  moduleCount;
            activityData[ActivityPropertyNames.ProjectCount] = projectCount;
        }

        return Task.CompletedTask;
    }
}