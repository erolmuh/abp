using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentInspection.Contracts;

public interface ISoftwareInfoProvider
{
    Task<List<SoftwareInfo>> GetSoftwareInfoAsync(CancellationToken cancellationToken = default);
}