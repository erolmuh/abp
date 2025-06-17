using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityStorage
{
    Guid InitializeOrGetSession();
    void MarkActivitiesAsSent();
    void SaveActivity(ActivityEvent activityEvent);
    List<ActivityEvent> GetActivities();
    bool ShouldAddDeviceInfo();
    bool ShouldAddSolutionInformation(Guid solutionId);
    bool ShouldAddProjectInfo(Guid projectId);
    bool ShouldSendActivities();
}