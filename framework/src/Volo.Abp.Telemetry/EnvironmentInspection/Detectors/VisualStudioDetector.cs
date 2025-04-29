using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class VisualStudioDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Visual Studio";

    public async override  Task<SoftwareInfo?> DetectAsync()
    {
        return null;
    }
}