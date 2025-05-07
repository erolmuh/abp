using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public class ModuleInfoContributor : ITelemetryApplicationInfoContributor
{
    public Task ContributeAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue(ActivityPropertyNameConstants.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath);
            var types = assembly.GetTypes();
            var moduleCount = types.Count(t => typeof(AbpModule).IsAssignableFrom(t) && !t.IsAbstract);
            var projectCount = types.Select(t => t.Assembly.GetName().Name).Distinct().Count();

            activityData.Add(ActivityPropertyNameConstants.ModuleCount, moduleCount);
            activityData.Add(ActivityPropertyNameConstants.ProjectCount, projectCount);
        }

        return Task.CompletedTask;
    }
}