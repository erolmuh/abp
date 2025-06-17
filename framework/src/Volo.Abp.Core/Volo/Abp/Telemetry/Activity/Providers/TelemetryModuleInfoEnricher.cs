using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Reflection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher), typeof(IHasParentTelemetryActivityEventEnricher))]
internal sealed class TelemetryModuleInfoEnricher : TelemetryActivityEventEnricher, IHasParentTelemetryActivityEventEnricher
{
    private readonly IModuleContainer _moduleContainer;
    private readonly IAssemblyFinder _assemblyFinder;

    public TelemetryModuleInfoEnricher(IModuleContainer moduleContainer, IAssemblyFinder assemblyFinder,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _moduleContainer = moduleContainer;
        _assemblyFinder = assemblyFinder;
    }

    public Type Parent => typeof(TelemetrySessionInfoEnricher);

    public override Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.SessionType == SessionType.ApplicationRuntime);
    }

    protected override Task ExecuteAsync(ActivityContext context)
    {
        context.Current[ActivityPropertyNames.ModuleCount] = _moduleContainer.Modules.Count;
        context.Current[ActivityPropertyNames.ProjectCount] = _assemblyFinder.Assemblies.Count(x =>
            !x.FullName.IsNullOrEmpty() && 
            !x.FullName.StartsWith(TelemetryConsts.VoloNameSpaceFilter));
        return Task.CompletedTask;
    }
}