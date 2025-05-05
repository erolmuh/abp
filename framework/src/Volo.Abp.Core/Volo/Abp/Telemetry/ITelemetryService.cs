using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public interface ITelemetryService
{
    IAsyncDisposable TrackActivity(string activityName, Action<ActivityData>? configure = null);
    IAsyncDisposable TrackActivity(ActivityData activityData);
    Task AddActivityAsync(ActivityData data, CancellationToken cancellationToken = default);
    Task AddActivityAsync(string activityName, string? details = null, CancellationToken cancellationToken = default);
}