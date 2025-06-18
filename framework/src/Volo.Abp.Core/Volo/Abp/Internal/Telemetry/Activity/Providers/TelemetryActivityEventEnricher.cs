using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;

namespace Volo.Abp.Internal.Telemetry.Activity.Providers;

public abstract class TelemetryActivityEventEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly IServiceProvider _serviceProvider;

    protected TelemetryActivityEventEnricher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public virtual int ExecutionOrder => 0;
    protected bool CancelChildren { get; set; }

    public virtual Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(true);
    }

    protected abstract Task ExecuteAsync(ActivityContext context);

    public async Task EnrichAsync(ActivityContext context)
    {
        if (!await CanExecuteAsync(context))
        {
            return;
        }

        await ExecuteAsync(context);

        if (!CancelChildren)
        {
            using var scope = _serviceProvider.CreateScope();
            var children = GetChildren(scope.ServiceProvider);

            foreach (var childEnricher in children)
            {
                await childEnricher.EnrichAsync(context);
            }
        }
    }

    private List<ITelemetryActivityEventEnricher> GetChildren(IServiceProvider serviceProvider)
    {
        return serviceProvider
            .GetRequiredService<IEnumerable<IHasParentTelemetryActivityEventEnricher>>()
            .Where(child => child.Parent == this.GetType())
            .Cast<ITelemetryActivityEventEnricher>()
            .ToList();
    }
}