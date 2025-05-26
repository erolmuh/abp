using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetrySolutionInfoProvider
{
    Task<IDictionary<string, object>> GetSolutionInfoAsync(string solutionPath);
    
    Guid? GetSolutionId(string solutionPath);
}