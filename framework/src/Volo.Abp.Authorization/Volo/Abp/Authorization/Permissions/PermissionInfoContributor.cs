using System.Threading.Tasks;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Authorization.Permissions;

public class PermissionInfoContributor : ITelemetryApplicationInfoContributor
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public PermissionInfoContributor(IPermissionDefinitionManager permissionDefinitionManager)
    {
        _permissionDefinitionManager = permissionDefinitionManager;
    }

    public async Task ContributeAsync(ActivityData activityData)
    {
        var permissions = await _permissionDefinitionManager.GetPermissionsAsync();
        activityData.Add(ActivityPropertyNameConstants.PermissionCount, permissions.Count);
    }
}