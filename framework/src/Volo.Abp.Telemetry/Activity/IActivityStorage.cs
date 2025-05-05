using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activity;

public interface IActivityStorage
{
    Task<DateTimeOffset?> GetLastActivitySendTimeAsync(CancellationToken cancellationToken = default);
    Task<(bool isFirstSession, Guid sessionId)> GetOrCreateSessionInfoAsync(CancellationToken cancellationToken = default);
    Task MarkActivitiesAsSentAsync(CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLastSolutionInfoSendTimeAsync(Guid solutionId, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetLastDeviceInfoSendTimeAsync(CancellationToken cancellationToken = default);
    Task MarkDeviceInfoAsSentAsync(CancellationToken cancellationToken = default);
    Task MarkSolutionInfoAsSentAsync(Guid solutionId , CancellationToken cancellationToken = default);
    Task BufferActivityAsync(ActivityData activityData, CancellationToken cancellationToken = default);
    Task<List<ActivityData>> GetBufferedActivitiesAsync(CancellationToken cancellationToken = default);
    Task MarkApplicationInfoAsSentAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<DateTimeOffset?> GetApplicationInfoLastActivitySendTimeAsync(Guid applicationId, CancellationToken cancellationToken = default);
}

