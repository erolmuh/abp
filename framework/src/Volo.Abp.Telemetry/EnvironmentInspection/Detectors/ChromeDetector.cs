using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvironmentInspection.Contracts;
using EnvironmentInspection.Enums;

namespace EnvironmentInspection.Detectors;

internal class ChromeDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Chrome";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
            if (File.Exists(chromePath))
            {
                return new SoftwareInfo(Name, GetFileVersion(chromePath), null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            if (File.Exists(chromePath))
            {
                var version = await ExecuteCommandAsync(chromePath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var chromePath = "/usr/bin/google-chrome";
            if (File.Exists(chromePath))
            {
                var version = await ExecuteCommandAsync(chromePath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }

        return null;
    }

  
}