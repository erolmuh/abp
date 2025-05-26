using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Shared;

namespace Volo.Abp.Authorization.Permissions;

[ExposeServices(typeof(ITelemetryApplicationInfoContributor))]
public class PermissionInfoContributor : ITelemetryApplicationInfoContributor, ISingletonDependency
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public PermissionInfoContributor(IPermissionDefinitionManager permissionDefinitionManager)
    {
        _permissionDefinitionManager = permissionDefinitionManager;
    }

    public async Task ContributeAsync(ActivityData activityData)
    {
        var permissions = await _permissionDefinitionManager.GetPermissionsAsync();
        activityData[ActivityPropertyName.PermissionCount] = permissions.Count;
    }
}