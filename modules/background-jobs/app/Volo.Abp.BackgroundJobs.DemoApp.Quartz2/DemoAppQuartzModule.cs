using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs.DemoApp.Shared;
using Volo.Abp.BackgroundJobs.Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Modularity;
using Volo.Abp.Quartz;

namespace Volo.Abp.BackgroundJobs.DemoApp.Quartz2;

[DependsOn(
    typeof(DemoAppSharedModule),
    typeof(AbpAutofacModule),
    typeof(AbpBackgroundJobsQuartzModule),
    typeof(AbpBackgroundWorkersQuartzModule)
)]
public class DemoAppQuartzModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        //https://github.com/quartznet/quartznet/blob/main/database/tables/tables_sqlServer.sql
        var configuration = context.Services.GetConfiguration();
        PreConfigure<AbpQuartzOptions>(options =>
        {
            options.Properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = context.Services.GetApplicationName(),
                ["quartz.jobStore.dataSource"] = "BackgroundJobsDemoApp",
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                ["quartz.serializer.type"] = "json",
                ["quartz.dataSource.BackgroundJobsDemoApp.connectionString"] = configuration.GetConnectionString("Default"),
                ["quartz.dataSource.BackgroundJobsDemoApp.provider"] = "SqlServer",
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz",
            };
        });
    }
}
