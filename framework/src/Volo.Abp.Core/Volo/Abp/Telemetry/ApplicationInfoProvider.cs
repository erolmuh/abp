using System.Linq;
using System.Reflection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Volo.Abp.Telemetry;

public class ApplicationInfoProvider : ISingletonDependency
{
    public ApplicationInfo Scan(Assembly assembly)
    {
        var types = assembly.GetTypes();

        return new ApplicationInfo
        {
            // EntityCount = types.Count(t => typeof(IEntity).IsAssignableFrom(t) && !t.IsAbstract),
            // AppServiceCount = types.Count(t => typeof(IApplicationService).IsAssignableFrom(t) && !t.IsAbstract),
            // ControllerCount = types.Count(t => typeof(AbpController).IsAssignableFrom(t) && !t.IsAbstract),
            // PermissionCount = types.Count(t => typeof(PermissionDefinitionProvider).IsAssignableFrom(t) && !t.IsAbstract),
            AbpModuleCount = types.Count(t => typeof(AbpModule).IsAssignableFrom(t) && !t.IsAbstract),
            ProjectCount = types.Select(t => t.Assembly.GetName().Name).Distinct().Count()
        };
    }
}