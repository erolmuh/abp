using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnvironmentInspection.Contracts;

namespace EnvironmentInspection;

internal class SoftwareInfoProvider : ISoftwareInfoProvider
{
    private readonly IEnumerable<ISoftwareDetector> _softwareDetectors;

    public SoftwareInfoProvider(IEnumerable<ISoftwareDetector> softwareDetectors)
    {
        _softwareDetectors = softwareDetectors;
    }

    public async Task<List<SoftwareInfo>> GetSoftwareInfoAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<SoftwareInfo>();

        foreach (var softwareDetector in _softwareDetectors)
        {
            try
            {
                var softwareInfo = await softwareDetector.DetectAsync();
                if (softwareInfo is not null)
                {
                    result.Add(softwareInfo);
                }
            }
            catch  
            {
                //ignored
            }
        }
        return result;
    }
  

}