using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Volo.Abp.Telemetry.Activity;

public static class ActivityEventExtensions
{
    public static ActivityEvent WithAdditionalProperty(this ActivityEvent activityEvent, string key, object value)
    {
        activityEvent.AdditionalProperties ??= new Dictionary<string, object>();
        activityEvent.AdditionalProperties[key] = value;
        return activityEvent;
    }

    public static ActivityEvent WithAdditionalProperties(this ActivityEvent activityEvent,
        Dictionary<string, object> properties)
    {
        activityEvent.AdditionalProperties ??= new Dictionary<string, object>();
        foreach (var property in properties)
        {
            activityEvent.AdditionalProperties[property.Key] = property.Value;
        }

        return activityEvent;
    }

    public static ActivityEvent WithAdditionalProperties(this ActivityEvent activityEvent,
        params (string key, object value)[] properties)
    {
        activityEvent.AdditionalProperties ??= new Dictionary<string, object>();
        foreach (var (key, value) in properties)
        {
            activityEvent.AdditionalProperties[key] = value;
        }

        return activityEvent;
    }

    public static ActivityEvent WithAdditionalProperties(this ActivityEvent activityEvent, object properties)
    {
        activityEvent.AdditionalProperties ??= new Dictionary<string, object>();

        foreach (var property in properties.GetType().GetProperties())
        {
            var value = property.GetValue(properties);
            if (value != null)
            {
                activityEvent.AdditionalProperties[property.Name] = value;
            }
        }

        return activityEvent;
    }
}