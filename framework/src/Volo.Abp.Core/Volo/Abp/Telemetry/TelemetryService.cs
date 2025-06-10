using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using ActivityEvent = Volo.Abp.Telemetry.Activity.ActivityEvent;

namespace Volo.Abp.Telemetry;

public class TelemetryService : ITelemetryService, ISingletonDependency
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

    public IAsyncDisposable TrackActivity(string activityName, Action<ActivityEvent>? configure = null)
    {
        Check.NotNullOrEmpty(activityName, nameof(activityName));
        var stopwatch = Stopwatch.StartNew();
        var activityData = new ActivityEvent(activityName);

        configure?.Invoke(activityData);

        return new AsyncDisposeFunc(async () =>
        {
            stopwatch.Stop();
            activityData.ActivityDuration = stopwatch.ElapsedMilliseconds;

            await AddActivityAsync(activityData);
        });
    }

    public IAsyncDisposable TrackActivity(ActivityEvent activityEvent)
    {
        var stopwatch = Stopwatch.StartNew();

        return new AsyncDisposeFunc(async () =>
        {
            stopwatch.Stop();
            activityEvent.ActivityDuration = stopwatch.ElapsedMilliseconds;
            await AddActivityAsync(activityEvent);
        });
    }



    public Task AddActivityAsync(ActivityEvent @event)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _telemetryActivityDataBuilder.BuildAsync(@event);
                await _telemetryActivityStorage.BufferActivityAsync(@event);

                if (@event.ActivityName == ActivityNameConsts.AbpStudioClose)
                {
                    await _telemetryActivityStorage.EndSessionAsync();
                }

                if (await _telemetryActivityStorage.ShouldSendActivitiesAsync())
                {
                    await _telemetryDataSender.SendAsync();
                }
            }
            catch
            {
                // ignored
            }
        });

        return Task.CompletedTask;
    }

    public async Task AddActivityAsync(string activityName, string? details = null)
    {
        await AddActivityAsync(new ActivityEvent(activityName, details));
    }

    public async Task AddActivityAsync(string activityName, Action<ActivityEvent> configure)
    {
        Check.NotNullOrEmpty(activityName, nameof(activityName));
        var activityData = new ActivityEvent(activityName);

        configure?.Invoke(activityData);

        await AddActivityAsync(activityData);
    }

    public async Task AddErrorActivityAsync(Action<Dictionary<string, object>> configure)
    {
        var activityData = new ActivityEvent(ActivityNameConsts.Error)
        {
            AdditionalProperties = new Dictionary<string, object>()
        };

        configure?.Invoke(activityData.AdditionalProperties);

        await AddActivityAsync(activityData);
    }

    public async Task AddErrorActivityAsync(string errorMessage)
    {
        var activityData = new ActivityEvent(ActivityNameConsts.Error)
        {
            AdditionalProperties = new Dictionary<string, object>
            {
                { "ErrorMessage", errorMessage },
            }
        };

        await AddActivityAsync(activityData);
    }

    public async Task AddErrorForActivityAsync(string failingActivity, string errorMessage)
    {
        var activityData = new ActivityEvent(ActivityNameConsts.Error)
        {
            AdditionalProperties = new Dictionary<string, object>
            {
                { "ErrorMessage", errorMessage },
                { "FailingActivity", failingActivity },
            }
        };

        await AddActivityAsync(activityData);
    }
   
}