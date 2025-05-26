using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetryActivityStorage
{
    Task<DateTimeOffset?> GetLastActivitySendTimeAsync();
    Task<(bool isFirstSession, Guid sessionId)> GetOrCreateSessionInfoAsync();
    Task MarkActivitiesAsSentAsync();
    Task MarkDeviceInfoAsSentAsync();
    Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId);
    Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync();
    Task BufferActivityAsync(ActivityData activityData);
    Task<List<ActivityData>> GetBufferedActivitiesAsync();
    Task EndSessionAsync();
    Task<bool> ShouldAddDeviceInfoAsync();
    Task<bool> ShouldAddSolutionInformation(Guid solutionId);
}