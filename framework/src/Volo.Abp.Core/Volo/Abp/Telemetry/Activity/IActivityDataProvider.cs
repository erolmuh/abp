using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityDataProvider
{
    Task AddDeviceInformationAsync(ActivityData activityData, CancellationToken cancellationToken = default);
    void AddApplicationInformation(ActivityData activityData, Assembly assembly);

    Task AddSolutionInformationAsync(ActivityData activityData, CancellationToken cancellationToken = default);
}