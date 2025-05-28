using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity.Contracts;

public interface ITelemetrySolutionInfoProvider
{
    Task FillSolutionInfoAsync(ActivityData activityData, string solutionPath);
    
    Guid? GetSolutionId(string solutionPath);
}