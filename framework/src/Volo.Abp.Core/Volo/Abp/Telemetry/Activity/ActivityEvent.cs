using System;
using System.Collections.Generic;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityEvent : Dictionary<string, object>
{
    public ActivityEvent()
    {
    }
    
    public ActivityEvent(string activityName, string? details = null)
    {
        Check.NotNullOrWhiteSpace(activityName, nameof(activityName));
        ActivityName = activityName;
        ActivityDetails = details;
        Time = DateTimeOffset.UtcNow;
        Id = Guid.NewGuid();
    }
    
    public bool HasSolutionInfo()
    {
        return this.ContainsKey(ActivityPropertyNames.HasSolutionInfo);
    }
    public bool HasDeviceInfo()
    {
        return this.ContainsKey(ActivityPropertyNames.HasDeviceInfo);
    }
    public bool HasProjectInfo()
    {
        return this.ContainsKey(ActivityPropertyNames.HasProjectInfo);
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

    public Guid Id {
        get => TryGetValue(nameof(Id), out var value) ? (Guid)value : Guid.NewGuid();
        set => this[nameof(Id)] = value;
    }
}