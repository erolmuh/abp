using System.Runtime.InteropServices;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class MsEdgeDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "MsEdge";
    public override async Task<SoftwareInfo?> DetectAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft", "Edge", "Application", "msedge.exe");
            
            if (File.Exists(firefoxPath))
            {
                return new SoftwareInfo(Name, GetFileVersion(firefoxPath), null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string edgePath = "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge";
            if (File.Exists(edgePath))
            {
                var version = await ExecuteCommandAsync(edgePath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string edgePath = "/usr/bin/microsoft-edge";
            if (File.Exists(edgePath))
            {
                var version = await ExecuteCommandAsync(edgePath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }

        return null;
    }
}