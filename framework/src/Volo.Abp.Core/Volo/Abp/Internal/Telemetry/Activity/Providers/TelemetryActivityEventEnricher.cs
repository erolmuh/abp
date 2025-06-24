using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;

namespace Volo.Abp.Internal.Telemetry.Activity.Providers;

public abstract class TelemetryActivityEventEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    public virtual int ExecutionOrder => 0;

    protected bool CancelChildren { get; set; }
    
    protected virtual Type? OverrideParentType { get; set; }

    private readonly IServiceProvider _serviceProvider;

    protected TelemetryActivityEventEnricher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task EnrichAsync(ActivityContext context)
    {
        if (!await CanExecuteAsync(context))
        {
            return;
        }

        await ExecuteAsync(context);
        await ExecuteChildren(context);
    }

    private async Task ExecuteChildren(ActivityContext context)
    {
        if (CancelChildren)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();

        foreach (var child in GetChildren(scope.ServiceProvider))
        {
            await child.EnrichAsync(context);
        }
    }

    public virtual Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(true);
    }

    protected abstract Task ExecuteAsync(ActivityContext context);

    private ITelemetryActivityEventEnricher[] GetChildren(IServiceProvider serviceProvider)
    {
        var lookupParentType = OverrideParentType ?? ProxyHelper.GetUnProxiedType(this);
        
        return serviceProvider
            .GetRequiredService<IEnumerable<IHasParentTelemetryActivityEventEnricher>>() //TODO: Performance and memory issues!
            .Where(child => child.ParentType == lookupParentType)
            .Cast<ITelemetryActivityEventEnricher>()
            .ToArray();
    }
}