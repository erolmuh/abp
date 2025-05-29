using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityData : Dictionary<string, object>
{
    public ActivityData()
    {
        Time = DateTimeOffset.UtcNow;
    }

    public ActivityData(string activityName, string? details = null)
    {
        if (activityName.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(activityName));
        }

        ActivityName = activityName;
        ActivityDetails = details;
        Time = DateTimeOffset.UtcNow;
    }

    public string ActivityName {
        get => TryGetValue(nameof(ActivityName), out var value) ? (string)value : string.Empty;
        set => this[nameof(ActivityName)] = value;
    }

    public string? ActivityDetails {
        get => TryGetValue(nameof(ActivityDetails), out var value) ? (string?)value : null;
        internal set {
            if (value is not null)
            {
                this[nameof(ActivityDetails)] = value;
            }
        }
    }

    public Dictionary<string, object>? AdditionalProperties {
        get => TryGetValue(nameof(AdditionalProperties), out var value) ? (Dictionary<string, object>?)value : null;
        set {
            if (value is not null)
            {
                this[nameof(AdditionalProperties)] = value;
            }
        }
    }

    public long? ActivityDuration {
        get => TryGetValue(nameof(ActivityDuration), out var value) ? (long?)value : null;
        internal set {
            if (value is not null)
            {
                this[nameof(ActivityDuration)] = value;
            }
        }
    }

    public DateTimeOffset Time {
        get => TryGetValue(nameof(Time), out var value) ? (DateTimeOffset)value : DateTimeOffset.UtcNow;
        set => this[nameof(Time)] = value;
    }
}