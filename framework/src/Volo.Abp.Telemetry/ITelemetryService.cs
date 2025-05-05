using System;
using System.Threading;
using System.Threading.Tasks;
using Activity;
using Volo.Abp.DependencyInjection;

public interface ITelemetryService : IScopedDependency
{
    IAsyncDisposable TrackActivity(string activityName, Action<ActivityData>? configure = null);
    IAsyncDisposable TrackActivity(ActivityData activityData);
    Task AddActivityAsync(ActivityData data, CancellationToken cancellationToken = default);
    Task AddActivityAsync(string activityName, string? details = null, CancellationToken cancellationToken = default);
}