using System;
using System.Collections.Generic;

namespace Volo.Abp.Telemetry.Activity;

public class ActivityStorageState
{
    public DateTimeOffset? ActivitySendTime { get; set; }
    public DateTimeOffset? LastDeviceInfoSendTime { get; set; }
    public Guid? SessionId { get; set; }
    public bool? IsFirstSession { get; set; }
    public List<ActivityData> Activities { get; set; } = new();
    public Dictionary<Guid,DateTimeOffset> Solutions { get; set; } = new();

    public Dictionary<Guid, DateTimeOffset> ApplicationInfos { get; set; } = new();
}



