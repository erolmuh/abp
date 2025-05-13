using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityStorage
{
    Task<DateTimeOffset?> GetLastActivitySendTimeAsync();
    Task<(bool isFirstSession, Guid sessionId)> GetOrCreateSessionInfoAsync();
    Task MarkActivitiesAsSentAsync();
    Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId);
    Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync();
    Task BufferActivityAsync(ActivityData activityData);
    Task<List<ActivityData>> GetBufferedActivitiesAsync();
    Task EndSessionAsync();
}

