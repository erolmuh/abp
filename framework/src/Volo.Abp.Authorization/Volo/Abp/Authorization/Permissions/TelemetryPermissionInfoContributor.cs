using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Authorization.Permissions;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class TelemetryPermissionInfoContributor : ITelemetryApplicationInfoContributor, ISingletonDependency
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public TelemetryPermissionInfoContributor(IPermissionDefinitionManager permissionDefinitionManager)
    {
        _permissionDefinitionManager = permissionDefinitionManager;
    }

    public async Task ContributeAsync(ActivityEvent activityEvent)
    {
        // TODO: How to exclude permissions coming from pre-built ABP modules? 
        var permissions = await _permissionDefinitionManager.GetPermissionsAsync();
        activityEvent[ActivityPropertyNames.PermissionCount] = permissions.Count;
    }
}