using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Activity.Providers;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Authorization.Permissions;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryPermissionInfoEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public TelemetryPermissionInfoEnricher(IPermissionDefinitionManager permissionDefinitionManager)
    {
        _permissionDefinitionManager = permissionDefinitionManager;
    }

    public bool IsFirstRun => false;
    public Type? DependsOn => typeof(TelemetryApplicationInfoEnricher);
    
    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.ProjectId.HasValue);
    }

    public async Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        var permissions = await _permissionDefinitionManager.GetPermissionsAsync();
        
        var result = new Dictionary<string, object>
        {
            { ActivityPropertyNames.PermissionCount, permissions.Count }
        };

        return result;
    }
}