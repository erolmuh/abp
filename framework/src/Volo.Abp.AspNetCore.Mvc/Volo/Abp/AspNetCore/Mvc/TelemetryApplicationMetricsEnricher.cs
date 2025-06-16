using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Reflection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Activity.Providers;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.AspNetCore.Mvc;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryApplicationMetricsEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly ITypeFinder _typeFinder;

    public TelemetryApplicationMetricsEnricher(ITypeFinder typeFinder)
    {
        _typeFinder = typeFinder;
    }

    public bool IsFirstRun => false;
    public Type? DependsOn => typeof(TelemetryApplicationInfoEnricher);
    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.SessionType == SessionType.ApplicationRuntime);
    }

    public Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        var appServiceCount = _typeFinder.Types.Count(t =>
            typeof(IApplicationService).IsAssignableFrom(t) &&
            t is { IsAbstract: false, IsInterface: false } && 
            !t.AssemblyQualifiedName!.StartsWith(TelemetryConsts.VoloNameSpaceFilter));

        var controllerCount = _typeFinder.Types.Count(t =>
            typeof(ControllerBase).IsAssignableFrom(t) &&
            !t.IsAbstract);


        var items = new Dictionary<string, object>()
        {
            {ActivityPropertyNames.AppServiceCount, appServiceCount},
            {ActivityPropertyNames.ControllerCount, controllerCount}
        };
        return Task.FromResult<Dictionary<string, object>?>(items);
    }
}