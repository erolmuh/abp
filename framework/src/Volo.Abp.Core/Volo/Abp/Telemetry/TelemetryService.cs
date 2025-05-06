using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public class TelemetryService :  ITelemetryService ,  IScopedDependency
{
    private readonly IActivityStorage _activityStorage;
    private readonly ITelemetryDataSender _telemetryDataSender;

    public TelemetryService(IActivityStorage activityStorage, ITelemetryDataSender telemetryDataSender)
    {
        _activityStorage = activityStorage;
        _telemetryDataSender = telemetryDataSender;
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

    private async Task CheckIfActivitySendTimeIsDueAsync()
    {
        var lastActivitySendTime = await _activityStorage.GetLastActivitySendTimeAsync();
        if (lastActivitySendTime is null)
        {
            await _telemetryDataSender.SendAsync();
        }
        
        if (lastActivitySendTime is not null && lastActivitySendTime > DateTimeOffset.UtcNow.AddDays(7) )
        {
            await _telemetryDataSender.SendAsync();
        }
    }
    public async Task AddActivityAsync(ActivityData data, CancellationToken cancellationToken = default)
    {
       
        await _activityStorage.BufferActivityAsync(data, cancellationToken);

        await CheckIfActivitySendTimeIsDueAsync();
    }

    public async Task AddActivityAsync(string activityName, string? details = null, CancellationToken cancellationToken = default)
    {
        await AddActivityAsync(new ActivityData(activityName, details),cancellationToken);
    }
}