using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs.DemoApp.Shared;
using Volo.Abp.BackgroundJobs.DemoApp.Shared.Jobs;
using Volo.Abp.BackgroundJobs.RabbitMQ;
using Volo.Abp.Modularity;

namespace Volo.Abp.BackgroundJobs.DemoApp.RabbitMq;

[DependsOn(
    typeof(DemoAppSharedModule),
    typeof(AbpAutofacModule),
    typeof(AbpBackgroundJobsRabbitMqModule)
)]
public class DemoAppRabbitMqModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<AbpRabbitMqBackgroundJobOptions>(options =>
        {
            options.DefaultQueueNamePrefix = context.Services.GetApplicationName()!.EndsWith('.') + options.DefaultQueueNamePrefix;
            options.DefaultDelayedQueueNamePrefix = context.Services.GetApplicationName()!.EndsWith('.') + options.DefaultDelayedQueueNamePrefix;
        });
    }

    public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var jobManager = context.ServiceProvider.GetRequiredService<IBackgroundJobManager>();
        await jobManager.EnqueueAsync(new RabbitMqJobArgs() { Value = "test 1 (rabbitmq)" });
    }
}
