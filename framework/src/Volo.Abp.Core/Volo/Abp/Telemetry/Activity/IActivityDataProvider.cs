using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityDataProvider
{
    
    Task AddExtraInformationAsync(ActivityData activity);
}