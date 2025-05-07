namespace Volo.Abp.Telemetry.Shared.Enums;

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