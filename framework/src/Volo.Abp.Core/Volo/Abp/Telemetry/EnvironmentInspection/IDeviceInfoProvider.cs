using System;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection;

public interface IDeviceInfoProvider 
{
    Task<Guid> GetDeviceIdAsync();
    OperationSystem GetOperatingSystem();
    DeviceType GetDeviceType();
    string GetLanguage(); 
    string GetCountry();
}