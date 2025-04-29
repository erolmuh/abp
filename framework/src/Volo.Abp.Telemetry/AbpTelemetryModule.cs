using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.Autofac;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Helpers;

namespace Volo.Abp.Telemetry;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpDddDomainModule),
    typeof(AbpDddApplicationContractsModule)
)]
public class AbpTelemetryModule : AbpModule
{

    public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var packageMetadata = AbpPackageMetadataHelper.GetMetaData(Assembly.GetCallingAssembly());
        
        if (packageMetadata != null)
        {
            var activityStorage = context.ServiceProvider.GetRequiredService<IActivityStorage>();
            var lastApplicationInfoSendTime = await activityStorage.GetApplicationInfoLastActivitySendTimeAsync(packageMetadata.ProjectId.To<Guid>());
            if (lastApplicationInfoSendTime is null ||
                DateTimeOffset.UtcNow - lastApplicationInfoSendTime > TimeSpan.FromDays(7))
            {
                var telemetryService = context.ServiceProvider.GetRequiredService<ITelemetryService>();
                
                await using var _ = telemetryService.TrackActivity(ActivityNameConsts.ApplicationRun, c =>
                {
                    c.Add("ProjectAssemblyForScan", Assembly.GetCallingAssembly());
                    c.Add("ProjectId", packageMetadata.ProjectId);
                    c.Add("ProjectType", packageMetadata.Role);
                });
            }
           
        }
    }

}

