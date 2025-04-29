using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using Volo.Abp.Cli;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.Telemetry.EnvironmentInspection;


public enum MobileApp
{
    Unknown = 0,
    None = 1,
    Maui = 2,
    ReactNative = 3
    
}


public enum SolutionTemplate
{
    Unknown,
    AppNoLayers,
    AppLayered,
    Microservice
}

public enum UiFramework
{
    Unknown = 0,
    None = 1,
    MvcRazorPages = 2,
    Angular = 3,
    BlazorWasm = 4,
    BlazorServer = 5,
    BlazorWebApp = 6,
    BlazorMaUI = 7,
}

public enum LicenseType
{
    Unknown,
    Free,
    Team,
    Business,
    Enterprise
}
public enum DatabaseProvider
{
    Unknown = 0,
    None = 1,
    EfCore = 2,
    MongoDb = 3
}

public enum Dbms
{
    Unknown = 0,
    None = 1,
    SqlServer = 2,
    PostgreSql = 3,
    Oracle = 4,
    OracleDevart = 5,
    MySql = 6,
    Sqlite = 7,
}
public enum UiTheme
{
    Unknown = 0,
    None = 1,
    Basic = 2,
    LeptonX = 3,
    LeptonXLite = 4
}

public enum UiThemeStyle
{
    Unknown = 0,
    System = 1,
    Dim = 2,
    Dark = 3,
    Light = 4
}


public enum AbpTool : byte
{
    Unknown = 0,
    StudioUI = 1,
    StudioCli = 2,
    OldCli = 3
}
public enum SoftwareType : byte
{
    Others = 0,
    AbpStudio = 1,
    DotnetSdk = 2,
    OperatingSystem = 3,
    Ide = 4,
    Browser = 5
}

public enum OperationSystem
{
    Unknown = 0,
    Windows = 1,
    MacOS = 2,
    Linux = 3,
}

public enum DeviceType
{
    Unknown = 0,
    Desktop = 1,
    Laptop = 2
}

public interface IDeviceInfoProvider 
{
    Task<Guid> GetDeviceIdAsync();
    OperationSystem GetOperatingSystem();
    DeviceType GetDeviceType();
    string GetLanguage(); 
    string? GetCountry();
}

public class DeviceInfoProvider : IDeviceInfoProvider , ISingletonDependency
{
    public async Task<Guid> GetDeviceIdAsync()
    {
        var  deviceId =
            await File.ReadAllTextAsync(Path.Combine(CliPaths.AbpRootPath, "cli", "computer-id.bin"));
        return deviceId.To<Guid>();
    }

    public OperationSystem GetOperatingSystem()
    {
        if (OperatingSystem.IsWindows()) return OperationSystem.Windows;
        if (OperatingSystem.IsLinux()) return OperationSystem.Linux;
        if (OperatingSystem.IsMacOS()) return OperationSystem.MacOS;
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

    public string? GetCountry()
    {
        try
        {
            var region = new RegionInfo(CultureInfo.CurrentUICulture.Name);
            return region.TwoLetterISORegionName;
        }
        catch
        {
            return null;
        }
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
                    return DeviceType.Laptop;
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
                return DeviceType.Laptop;
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

            if (output.Contains("MacBook")) return DeviceType.Laptop;
            if (output.Contains("iMac") || output.Contains("Mac Pro")) return DeviceType.Desktop;
        }
        catch
        {
            // ignored
        }

        return DeviceType.Unknown;
    }
}
