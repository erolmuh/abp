using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.EnvironmentInspection.Core;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

internal class FireFoxDetector : SoftwareDetector
{
    public override string Name => "Firefox";
    public async override Task<SoftwareInfo?> DetectAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var firefoxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe");
            if (File.Exists(firefoxPath))
            {
                return new SoftwareInfo(Name, GetFileVersion(firefoxPath), null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var firefoxPath = "/Applications/Firefox.app/Contents/MacOS/firefox";
            if (File.Exists(firefoxPath))
            {
                var version = await ExecuteCommandAsync(firefoxPath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var firefoxPath = "/usr/bin/firefox";
            if (File.Exists(firefoxPath))
            {
                var version = await ExecuteCommandAsync(firefoxPath, "--version");
                return new SoftwareInfo(Name, version, null, SoftwareType.Browser);
            }
        }

        return null;
    }
}