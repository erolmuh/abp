using System;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;
[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetrySessionInfoEnricher : TelemetryActivityEventEnricher 
{
    public TelemetrySessionInfoEnricher(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public override int ExecutionOrder => 10;

    protected override Task ExecuteAsync(ActivityContext context)
    {
        context.Current[ActivityPropertyNames.SessionType] = SessionType.ApplicationRuntime;
        context.Current[ActivityPropertyNames.SessionId] = Guid.NewGuid().ToString();
        context.Current[ActivityPropertyNames.IsFirstSession] = !File.Exists(TelemetryPaths.ActivityStorage);

        return Task.CompletedTask;
    }
}