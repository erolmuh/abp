using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvironmentInspection.Enums;
using Volo.Abp.DependencyInjection;

namespace EnvironmentInspection;

public class DeviceInfoProvider : IDeviceInfoProvider , ISingletonDependency
{
    public async Task<Guid> GetDeviceIdAsync()
    {
        var  deviceId =  await File.ReadAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".abp", "cli", "computer-id.bin"));
        return deviceId.To<Guid>();
    }

    public OperationSystem GetOperatingSystem()
    {
        if (OperatingSystem.IsWindows())
        {
            return OperationSystem.Windows;
        }

        if (OperatingSystem.IsLinux())
        {
            return OperationSystem.Linux;
        }

        if (OperatingSystem.IsMacOS())
        {
            return OperationSystem.MacOS;
        }

        return OperationSystem.Unknown;
    }

    public DeviceType GetDeviceType()
    {
        if (OperatingSystem.IsWindows())
        {
            return DetectDeviceTypeOnWindows();
        }

        if (OperatingSystem.IsLinux())
        {
            return DetectDeviceTypeOnLinux();
        }

        if (OperatingSystem.IsMacOS())
        {
            return DetectDeviceTypeOnMacOS();
        }

        if (RuntimeInformation.OSDescription.Contains("iOS", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.Laptop;
        }

        return DeviceType.Unknown;
    }

    public string GetLanguage()
    {
        return CultureInfo.CurrentUICulture.Name;
    }

    public string GetCountry()
    {
        var region = new RegionInfo(CultureInfo.CurrentUICulture.Name);
        return region.TwoLetterISORegionName;
    }

    private DeviceType DetectDeviceTypeOnWindows()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemEnclosure");
            foreach (var obj in searcher.Get())
            {
                var chassisTypes = obj["ChassisTypes"] as ushort[];
                if (chassisTypes != null && chassisTypes.Any(t => t is 8 or 9 or 10 or 14))
                {
                    return DeviceType.Laptop;
                }
            }
        }
        catch { }

        return DeviceType.Desktop;
    }

    private DeviceType DetectDeviceTypeOnLinux()
    {
        try
        {
            var type =  File.ReadAllText("/sys/class/dmi/id/chassis_type");
            if (int.TryParse(type.Trim(), out var code) && code is 8 or 9 or 10 or 14)
            {
                return DeviceType.Laptop;
            }
        }
        catch
        {
            // ignored
        }

        return DeviceType.Desktop;
    }

    private DeviceType DetectDeviceTypeOnMacOS()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "system_profiler",
                    Arguments = "SPHardwareDataType",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (output.Contains("MacBook"))
            {
                return DeviceType.Laptop;
            }

            if (output.Contains("iMac") || output.Contains("Mac Pro"))
            {
                return DeviceType.Desktop;
            }
        }
        catch
        {
            // ignored
        }

        return DeviceType.Unknown;
    }
}