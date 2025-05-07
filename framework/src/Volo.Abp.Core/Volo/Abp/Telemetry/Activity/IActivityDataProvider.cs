using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityDataProvider
{
    Task AddDeviceInformationAsync(ActivityData activityData);
    Task AddApplicationInformation(ActivityData activityData);
    Task AddSolutionInformationAsync(ActivityData activityData);
}