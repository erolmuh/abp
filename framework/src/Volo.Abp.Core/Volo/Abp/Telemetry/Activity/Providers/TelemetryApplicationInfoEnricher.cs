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

    public async Task EnrichAsync(ActivityEvent activity)
    {
        try
        {
            if (!ShouldEnrichActivity(activity))
            {
                return;
            }

            if (!TryGetProjectId(activity, out var projectId) && !await _telemetryActivityStorage.ShouldAddApplicationInfoAsync(projectId))
            {
                return;
            }

            foreach (var contributor in _telemetryApplicationInfoContributors)
            {
                await contributor.ContributeAsync(activity);
            }

            activity.Remove(ActivityPropertyNames.Assembly);
            activity[ActivityPropertyNames.HasProjectInfo] = true;
            await _telemetryActivityStorage.MarkApplicationInfoAsAddedAsync(projectId);
        }
        catch
        {
            // ignored
        }
    }

    private bool ShouldEnrichActivity(ActivityEvent activity)
    {
        return activity.ContainsKey(ActivityPropertyNames.Assembly)
               && activity.TryGetValue(ActivityPropertyNames.SessionType, out var sessionTypeObj)
               && Enum.TryParse<SessionType>(sessionTypeObj?.ToString(), out var parsed)
               && parsed == SessionType.ApplicationRuntime;
    }

    private static bool TryGetProjectId(ActivityEvent activity, out Guid projectId)
    {
        if (!activity.TryGetValue(ActivityPropertyNames.ProjectId, out var projectIdObj) ||
            projectIdObj is not string projectIdStr ||
            !Guid.TryParse(projectIdStr, out projectId))
        {
            projectId = Guid.Empty;
            return false;
        }

        return true;
    }
}