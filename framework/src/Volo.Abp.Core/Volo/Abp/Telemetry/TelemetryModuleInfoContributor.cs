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
    public Task ContributeAsync(ActivityEvent activityEvent)
    {
        // TODO: Exclude types with namespace "Volo."
        // TODO: Use ITypeFinder
        // TODO: Use IAssemblyFinder
        
        // TODO: Never use Assembly.LoadFrom (check all codebase)

        if (activityEvent.TryGetValue(ActivityPropertyNames.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath); 
            var types = assembly.GetTypes();
            
            //TODO: Use IModuleContainer to get modules
            var moduleCount = types.Count(t => typeof(IAbpModule).IsAssignableFrom(t) && !t.IsAbstract);
            var projectCount = types.Select(t => t.Assembly.GetName().Name).Distinct().Count();

            activityEvent[ActivityPropertyNames.ModuleCount] =  moduleCount;
            activityEvent[ActivityPropertyNames.ProjectCount] = projectCount;
        }

        return Task.CompletedTask;
    }
}