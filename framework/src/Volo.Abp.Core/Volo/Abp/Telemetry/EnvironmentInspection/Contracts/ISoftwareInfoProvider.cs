using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public interface ISoftwareInfoProvider
{
    Task<List<SoftwareInfo>> GetSoftwareInfoAsync(CancellationToken cancellationToken = default);
}