using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class ModuleInfoContributor : ITelemetryApplicationInfoContributor, ISingletonDependency
{
    public Task ContributeAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue(ActivityPropertyName.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath);
            var types = assembly.GetTypes();
            var moduleCount = types.Count(t => typeof(AbpModule).IsAssignableFrom(t) && !t.IsAbstract);
            var projectCount = types.Select(t => t.Assembly.GetName().Name).Distinct().Count();

            activityData[ActivityPropertyName.ModuleCount] =  moduleCount;
            activityData[ActivityPropertyName.ProjectCount] = projectCount;
        }

        return Task.CompletedTask;
    }
}

public interface ITelemetrySessionTypeProvider
{
    public SessionType SessionType { get;  }
}

public class ApplicationRuntimeSessionTypeProvider : ITelemetrySessionTypeProvider, ISingletonDependency
{
    public SessionType SessionType => SessionType.ApplicationRuntime;
}