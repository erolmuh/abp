using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
#if WINDOWS
using System.Management;
#endif

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityDataEnricher))]
public class TelemetryDeviceInfoEnricher : ITelemetryActivityDataEnricher, ISingletonDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private readonly ISoftwareInfoProvider _softwareInfoProvider;
    private readonly Lazy<Guid> _deviceId;

    public TelemetryDeviceInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage, ISoftwareInfoProvider softwareInfoProvider)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
        _softwareInfoProvider = softwareInfoProvider;
        _deviceId = new Lazy<Guid>(() =>
        {
            var deviceIdText = File.ReadAllText(TelemetryPaths.ComputerId);
            return deviceIdText.To<Guid>(); 
        });
    }

    public async Task<bool> ShouldEnrichAsync(ActivityData activity)
    {
        return await _telemetryActivityStorage.ShouldAddDeviceInfoAsync();
    }

    public async Task EnrichAsync(ActivityData activity)
    {
        
        activity[ActivityPropertyNames.DeviceId] = _deviceId.Value;
        if (!await _telemetryActivityStorage.ShouldAddDeviceInfoAsync())
        {
            return;
        }
        
        activity[ActivityPropertyNames.DeviceType] = GetDeviceType();
        activity[ActivityPropertyNames.DeviceLanguage] = GetLanguage();
        activity[ActivityPropertyNames.OperatingSystem] = GetOperatingSystem();
        activity[ActivityPropertyNames.CountryIsoCode] = GetCountry();

        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activity[ActivityPropertyNames.InstalledSoftwares] = softwareList;

        await _telemetryActivityStorage.MarkDeviceInfoAsAddedAsync();
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
        var culture = CultureInfo.CurrentUICulture;
        if (culture.IsNeutralCulture)
        {
            culture = CultureInfo.CreateSpecificCulture(culture.Name);
        }

        var region = new RegionInfo(culture.Name);
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
            var type = File.ReadAllText("/sys/class/dmi/id/chassis_type");
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