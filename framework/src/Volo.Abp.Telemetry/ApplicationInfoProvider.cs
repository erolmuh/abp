using Volo.Abp.Application.Services;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Modularity;
using Volo.Abp.Reflection;

namespace Volo.Abp.Telemetry;

public class ApplicationInfoProvider : ISingletonDependency
{
    private readonly ITypeFinder _typeFinder;


    public ApplicationInfoProvider(ITypeFinder typeFinder)
    {
        _typeFinder = typeFinder;
    }

    public ApplicationInfo Scan()
    {
        var types = _typeFinder.Types;

        return new ApplicationInfo
        {
            EntityCount = types.Count(t => typeof(IEntity).IsAssignableFrom(t) && !t.IsAbstract),
            AppServiceCount = types.Count(t => typeof(IApplicationService).IsAssignableFrom(t) && !t.IsAbstract),
            ControllerCount = types.Count(t => typeof(AbpController).IsAssignableFrom(t) && !t.IsAbstract),
            PermissionCount = types.Count(t => typeof(PermissionDefinitionProvider).IsAssignableFrom(t) && !t.IsAbstract),
            AbpModuleCount = types.Count(t => typeof(AbpModule).IsAssignableFrom(t) && !t.IsAbstract),
            ProjectCount = types.Select(t => t.Assembly.GetName().Name).Distinct().Count()
            
        };
    }
}