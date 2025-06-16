using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryDeviceInfoEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private readonly ISoftwareInfoProvider _softwareInfoProvider;

    public TelemetryDeviceInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage,
        ISoftwareInfoProvider softwareInfoProvider)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
        _softwareInfoProvider = softwareInfoProvider;
    }

    public bool IsFirstRun => true;
    public Type? DependsOn => null;
    
    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(true);
    }
    

    public async Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        try
        {
            var deviceId = DeviceKeyHelper.GetUniquePhysicalKey(true);
            
            if (!await _telemetryActivityStorage.ShouldAddDeviceInfoAsync())
            {
                context.Cancel();
                return new Dictionary<string, object>
                {
                    { ActivityPropertyNames.DeviceId, deviceId },
                };
            }
            
            var result = new Dictionary<string, object>
            {
                [ActivityPropertyNames.DeviceId] = deviceId,
                [ActivityPropertyNames.DeviceLanguage] = CultureInfo.CurrentUICulture.Name,
                [ActivityPropertyNames.OperatingSystem] = GetOperatingSystem(),
                [ActivityPropertyNames.CountryIsoCode] = GetCountry(),
                [ActivityPropertyNames.OperatingSystemArchitecture] =RuntimeInformation.OSArchitecture.ToString()
            };

            await EnrichWithSoftwareInfoAsync(result);
            result[ActivityPropertyNames.HasDeviceInfo] = true;
            return result;
            
        }
        catch
        {
            context.Terminate();
            return null;
            //ignored
        }
    }

    private async Task EnrichWithSoftwareInfoAsync(Dictionary<string,object> activity)
    {
        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activity[ActivityPropertyNames.InstalledSoftwares] = softwareList;
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