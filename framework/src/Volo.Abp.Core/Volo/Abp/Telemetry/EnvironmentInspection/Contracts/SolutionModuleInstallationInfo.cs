using System;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public class SolutionModuleInstallationInfo
{
    public string? ModuleName { get; set; } 
    public string? Version { get; set; }
    public DateTimeOffset? InstallationTime { get; set; }
}