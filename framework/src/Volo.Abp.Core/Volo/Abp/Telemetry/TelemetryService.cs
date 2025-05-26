using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public class TelemetryService : ITelemetryService, IScopedDependency
{
    private readonly IActivityStorage _activityStorage;
    private readonly ITelemetryDataSender _telemetryDataSender;
    private readonly IActivityDataProvider _activityDataProvider;

    public TelemetryService(IActivityStorage activityStorage, ITelemetryDataSender telemetryDataSender,
        IActivityDataProvider activityDataProvider)
    {
        _activityStorage = activityStorage;
        _telemetryDataSender = telemetryDataSender;
        _activityDataProvider = activityDataProvider;
    }

    public IAsyncDisposable TrackActivity(string activityName, Action<ActivityData>? configure = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var activityData = new ActivityData(activityName) { Time = DateTimeOffset.UtcNow };

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
        // if (lastActivitySendTime is null)
        // {
        //     await _telemetryDataSender.SendAsync();
        // }

        if (lastActivitySendTime is not null && lastActivitySendTime < DateTimeOffset.UtcNow.AddDays(-1))
        {
            await _telemetryDataSender.SendAsync();
        }
    }

    public async Task AddActivityAsync(ActivityData data)
    {
        try
        {
            await _activityDataProvider.AddExtraInformationAsync(data);
            await _activityStorage.BufferActivityAsync(data);
            await CheckIfActivitySendTimeIsDueAsync();

            if (data.ActivityName == ActivityNameConsts.AbpStudioClose)
            {
                await _activityStorage.EndSessionAsync();
            }
        }
        catch
        {
            // ignored
        }
    }

    public async Task AddActivityAsync(string activityName, string? details = null)
    {
        await AddActivityAsync(new ActivityData(activityName, details));
    }

    public async Task AddActivityAsync(string activityName, Action<ActivityData> configure)
    {
        var activityData = new ActivityData(activityName) { Time = DateTimeOffset.UtcNow };

        configure?.Invoke(activityData);

        await AddActivityAsync(activityData);
    }
}