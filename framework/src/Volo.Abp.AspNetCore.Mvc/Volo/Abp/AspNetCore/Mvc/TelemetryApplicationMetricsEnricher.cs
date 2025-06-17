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

[ExposeServices(typeof(ITelemetryActivityEventEnricher), typeof(IHasParentTelemetryActivityEventEnricher))]
public sealed class TelemetryApplicationMetricsEnricher : TelemetryActivityEventEnricher,
    IHasParentTelemetryActivityEventEnricher
{
    private readonly ITypeFinder _typeFinder;

    public TelemetryApplicationMetricsEnricher(ITypeFinder typeFinder, IServiceProvider serviceProvider) : base(
        serviceProvider)
    {
        _typeFinder = typeFinder;
    }

    public Type Parent => typeof(TelemetryApplicationInfoEnricher);

    public override Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.SessionType == SessionType.ApplicationRuntime);
    }

    protected override Task ExecuteAsync(ActivityContext context)
    {
        var appServiceCount = _typeFinder.Types.Count(t =>
            typeof(IApplicationService).IsAssignableFrom(t) &&
            t is { IsAbstract: false, IsInterface: false } &&
            !t.AssemblyQualifiedName!.StartsWith(TelemetryConsts.VoloNameSpaceFilter));

        var controllerCount = _typeFinder.Types.Count(t =>
            typeof(ControllerBase).IsAssignableFrom(t) &&
            !t.IsAbstract);


        context.Current[ActivityPropertyNames.AppServiceCount] = appServiceCount;
        context.Current[ActivityPropertyNames.ControllerCount] = controllerCount;
        return Task.CompletedTask;
    }
}