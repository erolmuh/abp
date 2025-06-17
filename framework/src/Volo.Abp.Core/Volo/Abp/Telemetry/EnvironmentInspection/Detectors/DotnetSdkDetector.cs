using System;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.EnvironmentInspection.Core;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal sealed class DotnetSdkDetector : SoftwareDetector
{
    public override string Name => "DotnetSdk";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        return new SoftwareInfo(Name, Environment.Version.ToString(), null, SoftwareType.DotnetSdk);
    }
}