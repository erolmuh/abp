using System;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public interface IDeviceInfoProvider 
{
    Guid GetDeviceId();
    OperationSystem GetOperatingSystem();
    DeviceType GetDeviceType();
    string GetLanguage(); 
    string GetCountry();
}