using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs.DemoApp.Shared;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.Modularity;

namespace Volo.Abp.BackgroundJobs.DemoApp2;

[DependsOn(
    typeof(DemoAppSharedModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule)
    )]
public class DemoAppModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<AbpBackgroundJobWorkerOptions>(options =>
        {
            options.ApplicationName = context.Services.GetApplicationName()!;
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(opts =>
            {
                opts.UseSqlServer();
            });
        });

        Configure<AbpBackgroundJobWorkerOptions>(options =>
        {
            //Configure for fast running
            options.ApplicationName = context.Services.GetApplicationName()!;
            options.JobPollPeriod = 1000;
            options.DefaultFirstWaitDuration = 1;
            options.DefaultWaitFactor = 1;
        });
    }

    public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        //TODO: Configure console logging
        //context
        //    .ServiceProvider
        //    .GetRequiredService<ILoggerFactory>()
        //    .AddConsole(LogLevel.Debug);

        await context.AddBackgroundWorkerAsync<MyWorker>();
        await context.AddBackgroundWorkerAsync<PassiveUserCheckerWorker>();
    }
}
