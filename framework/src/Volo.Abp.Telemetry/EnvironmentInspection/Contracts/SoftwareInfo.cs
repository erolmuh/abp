using System;
using EnvironmentInspection.Enums;

namespace EnvironmentInspection.Contracts;

public class SoftwareInfo(string name, string? version, string? uiTheme, SoftwareType softwareType)
{
    public string Name { get; set; } = name;
    public string? Version { get; set; } = version;
    public string? UiTheme { get; set; } = uiTheme;
    public SoftwareType SoftwareType { get; set; } = softwareType;
}

public class SolutionModuleInstallationInfo
{
    public required string ModuleName { get; set; } 
    public string? Version { get; set; }
    public DateTimeOffset? InstallationTime { get; set; }
}