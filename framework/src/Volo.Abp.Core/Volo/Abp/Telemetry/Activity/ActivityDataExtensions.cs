using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity;

public static class ActivityDataExtensions
{
    public static ActivityData WithAdditionalProperty(this ActivityData activityData, string key, object value)
    {
        activityData.AdditionalProperties ??= new Dictionary<string, object>();
        activityData.AdditionalProperties[key] = value;
        return activityData;
    }

    public static ActivityData WithAdditionalProperties(this ActivityData activityData, Dictionary<string, object> properties)
    {
        activityData.AdditionalProperties ??= new Dictionary<string, object>();
        foreach (var property in properties)
        {
            activityData.AdditionalProperties[property.Key] = property.Value;
        }
        return activityData;
    }

    public static ActivityData WithAdditionalProperties(this ActivityData activityData, params (string key, object value)[] properties)
    {
        activityData.AdditionalProperties ??= new Dictionary<string, object>();
        foreach (var (key, value) in properties)
        {
            activityData.AdditionalProperties[key] = value;
        }
        return activityData;
    }

    public static ActivityData WithAdditionalProperties(this ActivityData activityData, object properties)
    {
        activityData.AdditionalProperties ??= new Dictionary<string, object>();
        
        foreach (var property in properties.GetType().GetProperties())
        {
            var value = property.GetValue(properties);
            if (value != null)
            {
                activityData.AdditionalProperties[property.Name] = value;
            }
        }
        
        return activityData;
    }
} 