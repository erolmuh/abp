using System.Diagnostics;
using System.Reflection;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityData : Dictionary<string, object>
{
    public string ActivityName
    {
        get => (string) this[nameof(ActivityName)];
        init => this[nameof(ActivityName)] = value;
    }

    public string? ActivityDetails
    {
        get => (string?) this[nameof(ActivityDetails)];
        internal set
        {
            if (value is not null) this[nameof(ActivityDetails)] = value;
        }
    }

    public long? ActivityDuration
    {
        get => (long?) this[nameof(ActivityDuration)];
        internal set
        {
            if (value is not null) this[nameof(ActivityDuration)] = value;
        }
    }

    public DateTimeOffset Time = DateTimeOffset.UtcNow;

    public ActivityData(string activityName, string? details = null)
    {
        if (activityName.IsNullOrWhiteSpace())
            throw new ArgumentNullException(nameof(activityName));

        ActivityName = activityName;
        ActivityDetails = details;
    }
}

public static class ActivityPropertyNameConstants
{
    public const string SolutionId = nameof(SolutionId);
    public const string SessionId = nameof(SessionId);
    public const string ActivityDuration = nameof(ActivityDuration);
    public const string OperationSystem = nameof(OperationSystem);
    public const string DeviceType = nameof(DeviceType);
    public const string DeviceLanguage = nameof(DeviceLanguage);
}

public static class ActivityDataExtensions
{
    public static IAsyncDisposable BeginActivity(this ActivityData activity, ITelemetryService telemetryService)
    {
        var stopwatch = Stopwatch.StartNew();
        return new AsyncDisposeFunc(async () =>
        {
            stopwatch.Stop();
            activity.ActivityDuration = stopwatch.ElapsedMilliseconds;
            await telemetryService.AddActivityAsync(activity);
        });
    }
}

public class ApplicationRunActivityData : ActivityData
{
    public Assembly ProjectAssemblyForScan { get; set; }
    public ApplicationRunActivityData(Assembly projectAssemblyForScan) : base(ActivityNameConsts.ApplicationRun)
    {
        ProjectAssemblyForScan = projectAssemblyForScan;
    }
    
}