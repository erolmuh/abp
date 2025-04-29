using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class NodeJsDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Node.js";

    public override async Task<SoftwareInfo?> DetectAsync()
    {
        try
        {
            var output = await ExecuteCommandAsync("node", "-v");

            if (output.IsNullOrWhiteSpace())
                return null;

            var version = output.Trim().TrimStart('v');

            return new SoftwareInfo(Name, version, uiTheme: null, SoftwareType.Others);
        }
        catch
        {
            return null;
        }
    }
}