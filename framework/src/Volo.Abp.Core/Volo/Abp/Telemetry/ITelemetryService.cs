using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public interface ITelemetryService
{
    IAsyncDisposable TrackActivity(string activityName, Action<ActivityData>? configure = null);
    IAsyncDisposable TrackActivity(ActivityData activityData);
    Task AddActivityAsync(ActivityData data);
    Task AddActivityAsync(string activityName, string? details = null);
    Task AddActivityAsync(string activityName, Action<ActivityData> configure);
    Task AddErrorActivityAsync(Action<Dictionary<string, object>> configure);
    Task AddErrorActivityAsync(string errorMessage);
    Task AddErrorForActivityAsync(string failingActivity, string errorMessage);
}