using System.Runtime.InteropServices;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class FireFoxDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Firefox";
    public override async Task<SoftwareInfo?> DetectAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe");
            if (File.Exists(firefoxPath))
            {
                return new SoftwareInfo(Name, GetFileVersion(firefoxPath), null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string firefoxPath = "/Applications/Firefox.app/Contents/MacOS/firefox";
            if (File.Exists(firefoxPath))
            {
                var version = await ExecuteCommandAsync(firefoxPath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string firefoxPath = "/usr/bin/firefox";
            if (File.Exists(firefoxPath))
            {
                var version = await ExecuteCommandAsync(firefoxPath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }

        return null;
    }
}