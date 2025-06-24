using System;
using System.Collections.Generic;

namespace Volo.Abp.Internal.Telemetry.Activity.Contracts;

public interface ITelemetryActivityStorage
{
    Guid InitializeOrGetSession();
    void DeleteAcitivities();
    void SaveActivity(ActivityEvent activityEvent);
    List<ActivityEvent> GetActivities();
    bool ShouldAddDeviceInfo();
    bool ShouldAddSolutionInformation(Guid solutionId);
    bool ShouldAddProjectInfo(Guid projectId);
    bool ShouldSendActivities();
    void MarkActivitiesAsFailed(ActivityEvent[] activities);
}