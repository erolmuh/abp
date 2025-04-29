using System.Diagnostics;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public class TelemetryService : ApplicationService, ITelemetryService
{

    private readonly ILocalEventBus _localEventBus;
    public TelemetryService( ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }


    public IAsyncDisposable TrackActivity(string activityName, Action<ActivityData>? configure = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var activityData = new ActivityData(activityName)
        {
            Time = DateTimeOffset.UtcNow
        };

        configure?.Invoke(activityData);

        return new AsyncDisposeFunc(async () =>
        {
            stopwatch.Stop();
            activityData.ActivityDuration = stopwatch.ElapsedMilliseconds;

            await AddActivityAsync(activityData);
        });
    }

    public IAsyncDisposable TrackActivity(ActivityData activityData)
    {
        var stopwatch = Stopwatch.StartNew();
        activityData.Time = DateTimeOffset.UtcNow;

        return new AsyncDisposeFunc(async () =>
        {
            stopwatch.Stop();
            activityData.ActivityDuration = stopwatch.ElapsedMilliseconds;
            await AddActivityAsync(activityData);
        });
    }

  

    public async Task AddActivityAsync(ActivityData data, CancellationToken cancellationToken = default)
    {
        await _localEventBus.PublishAsync(data);
    }

    public async Task AddActivityAsync(string activityName, string? details = null, CancellationToken cancellationToken = default)
    {
        await AddActivityAsync(new ActivityData(activityName, details),cancellationToken);
    }
}