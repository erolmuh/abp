using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityData : Dictionary<string, object>
{
    public ActivityData()
    {
    }

    public ActivityData(string activityName, string? details = null)
    {
        if (activityName.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(activityName));
        }

        ActivityName = activityName;
        ActivityDetails = details;
    }

    public string ActivityName {
        get => (string)this[nameof(ActivityName)];
        set => this[nameof(ActivityName)] = value;
    }

    public string? ActivityDetails {
        get => (string?)this[nameof(ActivityDetails)];
        internal set {
            if (value is not null)
            {
                this[nameof(ActivityDetails)] = value;
            }
        }
    }

    public Dictionary<string, object>? AdditionalProperties {
        get => (Dictionary<string, object>?)this[nameof(AdditionalProperties)];
        set {
            if (value is not null)
            {
                this[nameof(AdditionalProperties)] = value;
            }
        }
    }

    public long? ActivityDuration {
        get => (long?)this[nameof(ActivityDuration)];
        internal set {
            if (value is not null)
            {
                this[nameof(ActivityDuration)] = value;
            }
        }
    }

    public DateTimeOffset Time = DateTimeOffset.UtcNow;
}