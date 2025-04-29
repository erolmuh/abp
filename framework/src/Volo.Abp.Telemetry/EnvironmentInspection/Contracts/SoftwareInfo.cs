namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public class SoftwareInfo(string name, string? version, string? uiTheme, SoftwareType softwareType)
{
    public string Name { get; set; } = name;
    public string? Version { get; set; } = version;
    public string? UiTheme { get; set; } = uiTheme;
    public SoftwareType SoftwareType { get; set; } = softwareType;
}