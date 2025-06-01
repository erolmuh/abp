using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityDataEnricher))]
public class TelemetryApplicationInfoEnricher : ITelemetryActivityDataEnricher, ISingletonDependency
{
    private readonly IEnumerable<ITelemetryApplicationInfoContributor> _telemetryApplicationInfoContributors;
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    public TelemetryApplicationInfoEnricher(
        IEnumerable<ITelemetryApplicationInfoContributor> telemetryApplicationInfoContributors,
        ITelemetryActivityStorage telemetryActivityStorage)
    {
        _telemetryApplicationInfoContributors = telemetryApplicationInfoContributors;
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    public async Task EnrichAsync(ActivityData activity)
    {
        if (!ShouldEnrichActivity(activity))
        {
            return;
        }

        var projectId = ExtractProjectId(activity);
        if (projectId == null)
        {
            return;
        }

        try
        {
            if (!await _telemetryActivityStorage.ShouldAddApplicationInfoAsync(projectId.Value))
            {
                return;
            }

            foreach (var contributor in _telemetryApplicationInfoContributors)
            {
                await contributor.ContributeAsync(activity);
            }

            activity.Remove(ActivityPropertyNames.Assembly);
            await _telemetryActivityStorage.MarkApplicationInfoAsAddedAsync(projectId.Value);
        }
        catch
        {
            // ignored
        }
    }

    private bool ShouldEnrichActivity(ActivityData activity)
    {
        return activity.ContainsKey(ActivityPropertyNames.Assembly)
               && activity.TryGetValue(ActivityPropertyNames.SessionType, out var sessionTypeObj)
               && Enum.TryParse<SessionType>(sessionTypeObj?.ToString(), out var parsed)
               && parsed == SessionType.ApplicationRuntime;
    }

    private static Guid? ExtractProjectId(ActivityData activity)
    {
        if (!activity.TryGetValue(ActivityPropertyNames.ProjectId, out var projectIdObj) ||
            projectIdObj is not string projectIdStr ||
            !Guid.TryParse(projectIdStr, out var projectId))
        {
            return null;
        }

        return projectId;
    }
}