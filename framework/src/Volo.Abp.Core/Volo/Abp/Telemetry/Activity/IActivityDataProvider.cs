using System.Threading;
using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityDataProvider
{
    Task<ActivityData> AddExtraInformationAsync(ActivityData activityData, CancellationToken cancellationToken = default);
}