using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using ActivityContext = Volo.Abp.Telemetry.Activity.ActivityContext;

namespace Volo.Abp.Telemetry; //TODO: Move Volo.Abp.Telemetry -> Volo.Abp.Internal.Telemetry 

public class TelemetryService : ITelemetryService, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private readonly ITelemetryActivitySender _telemetryActivitySender;
    private readonly ITelemetryActivityEventBuilder _telemetryActivityEventBuilder;

    public TelemetryService(ITelemetryActivityStorage telemetryActivityStorage,
        ITelemetryActivitySender telemetryActivitySender,
        ITelemetryActivityEventBuilder telemetryActivityEventBuilder)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
        _telemetryActivitySender = telemetryActivitySender;
        _telemetryActivityEventBuilder = telemetryActivityEventBuilder;
    }


    public IAsyncDisposable TrackActivityAsync(string activityName,
        Action<Dictionary<string, object>>? additionalProperties = null)
    {
        Check.NotNullOrEmpty(activityName, nameof(activityName));
        var stopwatch = Stopwatch.StartNew();
        var context = ActivityContext.Create(activityName);

        return new AsyncDisposeFunc(async () =>
        {
            stopwatch.Stop();
            context.Current[ActivityPropertyNames.ActivityDuration] = stopwatch.ElapsedMilliseconds;
            additionalProperties?.Invoke(context.Current);
            await AddActivityAsync(context);
        });
    }

    public async Task AddActivityAsync(string activityName,
        Action<Dictionary<string, object>>? additionalProperties = null)
    {
        Check.NotNullOrEmpty(activityName, nameof(activityName));
        var context = ActivityContext.Create(activityName, additionalProperties: additionalProperties);
        await AddActivityAsync(context);
    }

    public async Task AddErrorActivityAsync(Action<Dictionary<string, object>> additionalProperties)
    {
        var context = ActivityContext.Create(ActivityNameConsts.Error, additionalProperties: additionalProperties);
        await AddActivityAsync(context);
    }

    public async Task AddErrorActivityAsync(string errorMessage)
    {
        var context = ActivityContext.Create(ActivityNameConsts.Error, errorMessage);
        await AddActivityAsync(context);
    }

    public async Task AddErrorForActivityAsync(string failingActivity, string errorMessage)
    {
        Check.NotNullOrEmpty(failingActivity, nameof(failingActivity));
        var context = ActivityContext.Create(ActivityNameConsts.Error, errorMessage, configure =>
        {
            configure[ActivityPropertyNames.FailingActivity] = failingActivity;
        });
        await AddActivityAsync(context);
    }

    private async Task AddActivityAsync(ActivityContext context)
    {
        try
        {
            var activityEvent = await _telemetryActivityEventBuilder.BuildAsync(context);
            if (activityEvent is not null)
            {
                await _telemetryActivityStorage.BufferActivityAsync(activityEvent);
                if (await _telemetryActivityStorage.ShouldSendActivitiesAsync())
                {
                    await _telemetryActivitySender.SendAsync();
                }
            }
        }
        catch (Exception ex)
        {
           //ignored
        }
    }
}