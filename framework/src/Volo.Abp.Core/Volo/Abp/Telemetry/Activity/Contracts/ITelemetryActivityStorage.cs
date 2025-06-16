using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityStorage
{
    Task<Guid> InitializeOrGetSessionAsync();
    Task MarkActivitiesAsSentAsync();
    Task BufferActivityAsync(ActivityEvent activityEvent);
    Task<List<ActivityEvent>> GetBufferedActivitiesAsync();
    Task<bool> ShouldAddDeviceInfoAsync();
    Task<bool> ShouldAddSolutionInformation(Guid solutionId);
    Task<bool> ShouldAddProjectInfoAsync(Guid projectId);
    Task<bool> ShouldSendActivitiesAsync();
}