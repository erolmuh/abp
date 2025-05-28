using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry;

public class TelemetryService : ITelemetryService, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private readonly ITelemetryDataSender _telemetryDataSender;
    private readonly ITelemetryActivityDataBuilder _telemetryActivityDataBuilder;

    public TelemetryService(ITelemetryActivityStorage telemetryActivityStorage, ITelemetryDataSender telemetryDataSender,
        ITelemetryActivityDataBuilder telemetryActivityDataBuilder)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
        _telemetryDataSender = telemetryDataSender;
        _telemetryActivityDataBuilder = telemetryActivityDataBuilder;
    }

    public IAsyncDisposable TrackActivity(string activityName, Action<ActivityData>? configure = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var activityData = new ActivityData(activityName);

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

  

    public Task AddActivityAsync(ActivityData data)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _telemetryActivityDataBuilder.BuildAsync(data);
                await _telemetryActivityStorage.BufferActivityAsync(data);

                if (data.ActivityName == ActivityNameConsts.AbpStudioClose)
                {
                    await _telemetryActivityStorage.EndSessionAsync();
                }

                await SendActivityIfDueAsync();
            }
            catch
            {
                // ignored
            }
        });

        return Task.CompletedTask;
    }

    public async Task AddActivityAsync(string activityName, string? detail = null)
    {
        await AddActivityAsync(new ActivityData(activityName, detail));
    }

    public async Task AddActivityAsync(string activityName, Action<ActivityData> configure)
    {
        var activityData = new ActivityData(activityName);

        configure?.Invoke(activityData);

        await AddActivityAsync(activityData);
    }

    public async Task AddErrorActivityAsync(Action<Dictionary<string,object>> configure)
    {
        var activityData = new ActivityData(ActivityNameConsts.Error)
        {
            ActivityDetails = new Dictionary<string, object>()
        };

        configure?.Invoke(activityData.ActivityDetails);

        await AddActivityAsync(activityData);
    }

    public async Task AddErrorForActivityAsync(string failingActivity, string errorMessage)
    {
        var activityData = new ActivityData(ActivityNameConsts.Error)
        {
            ActivityDetails = new Dictionary<string, object>
            {
                { "ErrorMessage", errorMessage },
                { "FailingActivity", failingActivity },
            }
        };

        await AddActivityAsync(activityData);
    }
    private async Task SendActivityIfDueAsync()
    {
        var lastActivitySendTime = await _telemetryActivityStorage.GetLastActivitySendTimeAsync();

        if (lastActivitySendTime is not null && lastActivitySendTime < DateTimeOffset.UtcNow.AddDays(-1))
        {
            await _telemetryDataSender.SendAsync();
        }
    }
}