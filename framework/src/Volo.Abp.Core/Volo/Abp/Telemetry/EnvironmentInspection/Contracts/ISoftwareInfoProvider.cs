using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

internal interface ISoftwareInfoProvider
{
    Task<List<SoftwareInfo>> GetSoftwareInfoAsync();
}