using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EnvironmentInspection.Contracts;
using EnvironmentInspection.Enums;
using Microsoft.Win32;

namespace EnvironmentInspection.Detectors;

internal class OperatingSystemDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : "Linux";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            return new SoftwareInfo(Name, Environment.OSVersion.Version.ToString(),GetWindowsUiTheme(), SoftwareType.OperatingSystem);
        }

        if (OperatingSystem.IsMacOS())
        {
            var version = await ExecuteCommandAsync("sw_vers", "-productVersion");
            return new SoftwareInfo(Name, version, GetMacUiTheme(), SoftwareType.OperatingSystem);
        }

        if (OperatingSystem.IsLinux())
        {
            var version = await ExecuteCommandAsync("lsb_release","-ds") ?? await ExecuteCommandAsync("uname", "-r");
            return new SoftwareInfo(Name, version, await GetLinuxUiTheme(), SoftwareType.OperatingSystem);
        }

        return null;
    }

    private async Task<string?> GetLinuxUiTheme()
    {
        var output = await ExecuteCommandAsync("gsettings", "get org.gnome.desktop.interface gtk-theme");

        if (!output.IsNullOrWhiteSpace() && output.ToLowerInvariant().Contains("dark"))
        {
            return "Dark";
        }

        return "Light";
    }


    private string? GetMacUiTheme()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = "read -g AppleInterfaceStyle",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return output == "Dark" ? "Dark" : "Light";
        }
        catch
        {
            return "Light"; 
        }
    }
    private  string? GetWindowsUiTheme()
    {
        try
        {
            const string key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string value = "AppsUseLightTheme"; 

            using var registryKey = Registry.CurrentUser.OpenSubKey(key);
            var result = registryKey?.GetValue(value);

            return result switch
            {
                0 => "Dark",
                1 => "Light",
                _ => null
            };
        }
        catch (Exception e)
        {
            return null;
        }
    }
}