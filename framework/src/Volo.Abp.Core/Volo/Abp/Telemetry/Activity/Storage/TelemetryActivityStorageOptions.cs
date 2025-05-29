using System;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorageOptions
{
    public TimeSpan InfoExpirationPeriod { get; set; } = TimeSpan.FromDays(7);

    public TimeSpan ActivitySendPeriod { get; set; } = TimeSpan.FromDays(1);
} 