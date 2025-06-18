using System;
using System.Text.Json;

namespace Volo.Abp.Internal.Telemetry.Activity;

static internal class JsonElementExtensions
{
    static internal string GetString(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    static internal bool GetBoolean(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               bool.TryParse(property.GetString(), out var b) && b;
    }

    static internal bool TryGetDateTimeOffset(this JsonElement element, string propertyName , out DateTimeOffset result)
    {
        result = default;
        return element.TryGetProperty(propertyName, out var ct) && DateTimeOffset.TryParse(ct.GetString()!, out result);
    }
}