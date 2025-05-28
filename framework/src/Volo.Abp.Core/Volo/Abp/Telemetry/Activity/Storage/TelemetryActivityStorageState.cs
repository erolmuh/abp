using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity.Storage;

public class TelemetryActivityStorageState
{
    public DateTimeOffset? ActivitySendTime { get; set; }
    public DateTimeOffset? LastDeviceInfoAddTime { get; set; }
    public Guid? SessionId { get; set; }
    public List<ActivityData> Activities { get; set; } = new();
    public Dictionary<Guid,DateTimeOffset> Solutions { get; set; } = new();

    public Dictionary<Guid, DateTimeOffset> ApplicationInfos { get; set; } = new();
}



