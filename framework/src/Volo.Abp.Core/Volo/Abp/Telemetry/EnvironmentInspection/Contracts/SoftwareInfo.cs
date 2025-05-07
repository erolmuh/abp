using System;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public class SoftwareInfo(string name, string? version, string? uiTheme, SoftwareType softwareType)
{
    public string Name { get; set; } = name;
    public string? Version { get; set; } = version;
    public string? UiTheme { get; set; } = uiTheme;
    public SoftwareType SoftwareType { get; set; } = softwareType;
}

public class SolutionModuleInstallationInfo
{
    public string? ModuleName { get; set; } 
    public string? Version { get; set; }
    public DateTimeOffset? InstallationTime { get; set; }
}