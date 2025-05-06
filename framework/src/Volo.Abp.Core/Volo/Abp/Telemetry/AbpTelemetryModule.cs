// using System;
// using System.Reflection;
// using System.Threading.Tasks;
// using Microsoft.Extensions.DependencyInjection;
// using Volo.Abp.Modularity;
// using Volo.Abp.Telemetry.Activity;
// using Volo.Abp.Telemetry.Helpers;
//
// namespace Volo.Abp.Telemetry;
//
// public class AbpTelemetryModule : AbpModule
// {
//
//     public async override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
//     {
//         var packageMetadata = AbpPackageMetadataHelper.GetMetaData(Assembly.GetCallingAssembly());
//         
//         if (packageMetadata != null)
//         {
//            
//                 var telemetryService = context.ServiceProvider.GetRequiredService<ITelemetryService>();
//                 
//                 await using var _ = telemetryService.TrackActivity(ActivityNameConsts.ApplicationRun, activity =>
//                 {
//                     activity.Add("ProjectAssemblyForScan", Assembly.GetCallingAssembly());
//                     activity.Add("ProjectId", packageMetadata.ProjectId!);
//                     activity.Add("ProjectType", packageMetadata.Role!);
//                     activity.Add("SolutionPath", packageMetadata.AbpSlnPath ?? string.Empty);
//                 });
//            
//         }
//     }
//
// }