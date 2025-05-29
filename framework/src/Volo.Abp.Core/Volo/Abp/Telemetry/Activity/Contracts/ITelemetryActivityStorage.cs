using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityStorage
{
    Task<DateTimeOffset?> GetLastActivitySendTimeAsync();
    Task<Guid> GetOrCreateSessionInfoAsync();
    Task MarkActivitiesAsSentAsync();
    Task MarkDeviceInfoAsAddedAsync();
    Task MarkSolutionInfoAsAddedAsync(Guid solutionId);
    Task MarkApplicationInfoAsAddedAsync(Guid applicationInfo);
    Task BufferActivityAsync(ActivityData activityData);
    Task<List<ActivityData>> GetBufferedActivitiesAsync();
    Task EndSessionAsync();
    Task<bool> ShouldAddDeviceInfoAsync();
    Task<bool> ShouldAddSolutionInformation(Guid solutionId);
    Task<bool> ShouldAddApplicationInfoAsync(Guid applicationId);
}