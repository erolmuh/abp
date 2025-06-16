using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorageState
{
    public DateTimeOffset? ActivitySendTime { get; set; }
    public DateTimeOffset? LastDeviceInfoAddTime { get; set; }
    public Guid? SessionId { get; set; }
    public List<ActivityEvent> Activities { get; set; } = new();
    public Dictionary<Guid,DateTimeOffset> Solutions { get; set; } = new();
    public Dictionary<Guid, DateTimeOffset> Projects { get; set; } = new();
}



