using System.Reflection;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;
using System.Linq;

namespace Volo.Abp.AspNetCore.Mvc;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class TelemetryApplicationMetricsContributor : ITelemetryApplicationInfoContributor, ISingletonDependency
{
    public Task ContributeAsync(ActivityData activityData)
    {
        if (activityData.TryGetValue(ActivityPropertyNames.Assembly, out var assemblyPath))
        {
            var assembly = Assembly.LoadFrom((string)assemblyPath);
            var types = assembly.GetTypes();

            var appServiceCount = types.Count(t =>
                typeof(IApplicationService).IsAssignableFrom(t) &&
                t is { IsAbstract: false, IsInterface: false });

            var controllerCount = types.Count(t =>
                typeof(AbpController).IsAssignableFrom(t) &&
                !t.IsAbstract);

            activityData[ActivityPropertyNames.AppServiceCount] = appServiceCount;
            activityData[ActivityPropertyNames.ControllerCount] = controllerCount;
        }
        return Task.CompletedTask;
    }
}