using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs.DemoApp.Shared;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BackgroundWorkers.Hangfire;
using Volo.Abp.Hangfire;
using Volo.Abp.Modularity;

namespace Volo.Abp.BackgroundJobs.DemoApp.HangFire2;

[DependsOn(
    typeof(DemoAppSharedModule),
    typeof(AbpAutofacModule),
    typeof(AbpBackgroundJobsHangfireModule),
    typeof(AbpBackgroundWorkersHangfireModule)
)]
public class DemoAppHangfireModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        PreConfigure<IGlobalConfiguration>(hangfireConfiguration =>
        {
            hangfireConfiguration.UseSqlServerStorage(configuration.GetConnectionString("Default"));
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpHangfireOptions>(options =>
        {
            options.DefaultQueuePrefix = context.Services.GetApplicationName()!;
            options.ServerOptions ??= new BackgroundJobServerOptions();
            options.ServerOptions.Queues = new[] { "default", "my_queue" };
        });

        var configuration = context.Services.GetConfiguration();
        context.Services.AddHangfire(config =>
        {
            config.UseSqlServerStorage(configuration.GetConnectionString("Default"));
        });
    }

    public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<MyLogWorker>();
        await context.AddBackgroundWorkerAsync<MyLogWorker2>();
    }
}
