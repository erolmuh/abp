using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class DotnetSdkDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "DotnetSdk";

    public override async Task<SoftwareInfo?> DetectAsync()
    {
        var output = await ExecuteCommandAsync("dotnet", "--version");
        if (output.IsNullOrWhiteSpace())
        {
            return null;
        }

        return new SoftwareInfo(Name, output, null, SoftwareType.DotnetSdk);
    }
}