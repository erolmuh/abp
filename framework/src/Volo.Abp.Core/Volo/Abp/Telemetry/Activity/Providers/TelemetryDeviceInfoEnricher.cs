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

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryDeviceInfoEnricher : ITelemetryActivityEventEnricher, ISingletonDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private readonly ISoftwareInfoProvider _softwareInfoProvider;

    public TelemetryDeviceInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage,
        ISoftwareInfoProvider softwareInfoProvider)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
        _softwareInfoProvider = softwareInfoProvider;
    }

    public async Task EnrichAsync(ActivityEvent activity)
    {
        try
        {
            if (!await _telemetryActivityStorage.ShouldAddDeviceInfoAsync())
            {
                return;
            }

            EnrichWithDeviceInfo(activity);
            await EnrichWithSoftwareInfoAsync(activity);
            activity[ActivityPropertyNames.HasDeviceInfo] = true;
            await _telemetryActivityStorage.MarkDeviceInfoAsAddedAsync();
        }
        catch
        {
            //ignored
        }
    }

    private void EnrichWithDeviceInfo(ActivityEvent activity)
    {
        if (TryGetDeviceId(out var deviceId))
        {
            activity[ActivityPropertyNames.DeviceId] = deviceId;
        }

        activity[ActivityPropertyNames.DeviceLanguage] = CultureInfo.CurrentUICulture.Name;
        activity[ActivityPropertyNames.OperatingSystem] = GetOperatingSystem();
        activity[ActivityPropertyNames.CountryIsoCode] = GetCountry();
    }

    private async Task EnrichWithSoftwareInfoAsync(ActivityEvent activity)
    {
        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activity[ActivityPropertyNames.InstalledSoftwares] = softwareList;
    }

    private static bool TryGetDeviceId(out Guid deviceId)
    {
        try
        {
            if (File.Exists(TelemetryPaths.ComputerId))
            {
                var deviceIdText = File.ReadAllText(TelemetryPaths.ComputerId);
                deviceId = deviceIdText.To<Guid>();
                return true;
            }
        }
        catch
        {
            // ignored
        }

        deviceId = Guid.Empty;
        return false;
    }

    private static OperationSystem GetOperatingSystem()
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

  
    private static string GetCountry()
    {
        var culture = CultureInfo.CurrentUICulture;
        if (culture.IsNeutralCulture)
        {
            culture = CultureInfo.CreateSpecificCulture(culture.Name);
        }

        var region = new RegionInfo(culture.Name);
        return region.TwoLetterISORegionName;
    }
}