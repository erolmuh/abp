using System;

namespace Volo.Abp.Internal.Telemetry.Activity.Storage;

public class FailedActivityInfo
{
    public DateTimeOffset FirstFailTime { get; set; }
    public DateTimeOffset LastFailTime { get; set; }
    public int RetryCount { get; set; }
}