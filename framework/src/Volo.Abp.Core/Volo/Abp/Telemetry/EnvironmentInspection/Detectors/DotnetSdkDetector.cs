using System;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.EnvironmentInspection.Core;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class DotnetSdkDetector : SoftwareDetector
{
    public override string Name => "DotnetSdk";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        var output = await ExecuteCommandAsync("dotnet", "--version"); //TODO: Get from Environment or somewhere else
        if (output.IsNullOrWhiteSpace())
        {
            return null;
        }

        return new SoftwareInfo(Name, output, null, SoftwareType.DotnetSdk);
    }
}