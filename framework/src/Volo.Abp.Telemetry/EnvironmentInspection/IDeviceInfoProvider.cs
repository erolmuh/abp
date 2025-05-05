using System;
using System.Threading.Tasks;
using EnvironmentInspection.Enums;

namespace EnvironmentInspection;

public interface IDeviceInfoProvider 
{
    Task<Guid> GetDeviceIdAsync();
    OperationSystem GetOperatingSystem();
    DeviceType GetDeviceType();
    string GetLanguage(); 
    string GetCountry();
}