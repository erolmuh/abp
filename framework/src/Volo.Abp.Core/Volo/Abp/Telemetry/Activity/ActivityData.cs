using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityData : Dictionary<string, object>
{
    public ActivityData()
    {
    }

    public ActivityData(string activityName, string? detail = null)
    {
        if (activityName.IsNullOrWhiteSpace())
        {
            throw new ArgumentNullException(nameof(activityName));
        }

        ActivityName = activityName;
        ActivityDetail = detail;
    }

    public string ActivityName {
        get => (string)this[nameof(ActivityName)];
        set => this[nameof(ActivityName)] = value;
    }

    public string? ActivityDetail {
        get => (string?)this[nameof(ActivityDetail)];
        internal set {
            if (value is not null)
            {
                this[nameof(ActivityDetail)] = value;
            }
        }
    }

    public Dictionary<string, object>? ActivityDetails {
        get => (Dictionary<string, object>?)this[nameof(ActivityDetail)];
        internal set {
            if (value is not null)
            {
                this[nameof(ActivityDetail)] = value;
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