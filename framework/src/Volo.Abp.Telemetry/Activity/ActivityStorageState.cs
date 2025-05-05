using System;
using System.Collections.Generic;

namespace Activity;

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



public class ApplicationInfo
{
    public Guid ApplicationId { get; set; }
    public ApplicationType Type { get; set; }
    public int EntityCount { get; set; } // Number of entities in the application, to understand its size
    public int AppServiceCount { get; set; } // ...
    public int ControllerCount { get; set; } // ...
    public int PermissionCount { get; set; } // ...
    public int AbpModuleCount { get;  set; }

}

public enum ApplicationType
{
    Unknown = 0,
    Console = 1,
    MvcUi = 2,
    BlazorServerUi = 3,
    BlazorWebAssemblyUi = 4,
    BlazorWebAppUi = 5,
    HttpApi = 6,
    AspNetCore = 7 //We could not understand if it is HTTP API or MVC UI or whatever
    //TODO: More?
}