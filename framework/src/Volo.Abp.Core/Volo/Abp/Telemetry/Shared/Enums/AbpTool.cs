namespace Volo.Abp.Telemetry.Shared.Enums;

public enum AbpTool : byte
{
    Unknown = 0,
    StudioUI = 1,
    StudioCli = 2,
    OldCli = 3
}

public enum SessionType
{
    Unknown = 0,
    AbpStudio = 1,
    AbpCli = 2,
    ApplicationRuntime = 3
} 