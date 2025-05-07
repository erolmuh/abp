using System;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class DotnetSdkDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "DotnetSdk";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        var output = await ExecuteCommandAsync("dotnet", "--version");
        if (output.IsNullOrWhiteSpace())
        {
            return null;
        }

        return new SoftwareInfo(Name, output, null, SoftwareType.DotnetSdk);
    }
}