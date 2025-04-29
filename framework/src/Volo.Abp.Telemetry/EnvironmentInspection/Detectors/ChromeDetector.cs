using System.Runtime.InteropServices;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class ChromeDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Chrome";

    public override async Task<SoftwareInfo?> DetectAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
            if (File.Exists(chromePath))
            {
                return new SoftwareInfo(Name, GetFileVersion(chromePath), null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            if (File.Exists(chromePath))
            {
                var version = await ExecuteCommandAsync(chromePath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string chromePath = "/usr/bin/google-chrome";
            if (File.Exists(chromePath))
            {
                var version = await ExecuteCommandAsync(chromePath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }

        return null;
    }

  
}