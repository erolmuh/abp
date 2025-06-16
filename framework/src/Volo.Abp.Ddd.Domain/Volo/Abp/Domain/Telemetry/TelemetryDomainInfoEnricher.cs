using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Reflection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Activity.Providers;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Domain.Telemetry;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryDomainInfoEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly ITypeFinder _typeFinder;

    public TelemetryDomainInfoEnricher(ITypeFinder typeFinder)
    {
        _typeFinder = typeFinder;
    }
    public bool IsFirstRun => false;
    public Type? DependsOn => typeof(TelemetryApplicationInfoEnricher);

    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.ProjectId.HasValue);
    }

    public Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        var entityCount = _typeFinder.Types.Count(t =>
            typeof(IEntity).IsAssignableFrom(t) && !t.IsAbstract &&
            !t.AssemblyQualifiedName!.StartsWith(TelemetryConsts.VoloNameSpaceFilter));


        var result = new Dictionary<string, object>() { { ActivityPropertyNames.EntityCount, entityCount } };
        return Task.FromResult<Dictionary<string, object>?>(result);
    }
}