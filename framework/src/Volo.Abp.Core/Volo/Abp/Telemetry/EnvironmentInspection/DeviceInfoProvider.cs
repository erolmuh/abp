using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Shared;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.EnvironmentInspection.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection;

public class DeviceInfoProvider : IDeviceInfoProvider , ISingletonDependency
{
    public Task<Guid> GetDeviceIdAsync()
    {
        var  deviceId =  File.ReadAllText(AbpTelemetryPaths.ComputerId);
        return Task.FromResult(deviceId.To<Guid>());
    }

    public OperationSystem GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OperationSystem.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OperationSystem.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OperationSystem.MacOS;
        }

        return OperationSystem.Unknown;
    }

    public DeviceType GetDeviceType()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return DetectDeviceTypeOnWindows();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return DetectDeviceTypeOnLinux();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return DetectDeviceTypeOnMacOS();
        }

        if (RuntimeInformation.OSDescription.ToLower().Contains("ios"))
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