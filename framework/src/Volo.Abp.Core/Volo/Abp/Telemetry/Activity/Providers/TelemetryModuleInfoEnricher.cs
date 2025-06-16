using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Reflection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryModuleInfoEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{

    private readonly IModuleContainer _moduleContainer;
    private readonly IAssemblyFinder _assemblyFinder;
    public TelemetryModuleInfoEnricher(IModuleContainer moduleContainer, IAssemblyFinder assemblyFinder)
    {
        _moduleContainer = moduleContainer;
        _assemblyFinder = assemblyFinder;
    }
    
    public bool IsFirstRun => false;
    public Type? DependsOn => typeof(TelemetrySessionInfoEnricher);
    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.SessionType == SessionType.ApplicationRuntime);
    }

    public Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        var result = new Dictionary<string, object>
        {
            { ActivityPropertyNames.ModuleCount, _moduleContainer.Modules.Count },
            { ActivityPropertyNames.ProjectCount, _assemblyFinder.Assemblies.Count(x => !x.FullName.IsNullOrEmpty() && !x.FullName.StartsWith(TelemetryConsts.VoloNameSpaceFilter)) }
        };

        return Task.FromResult<Dictionary<string, object>?>(result);
    }
}