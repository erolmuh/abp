using System.Threading;
using System.Threading.Tasks;

namespace Activity;

public interface IActivityDataProvider
{
    Task<ActivityData> AddExtraInformationAsync(ActivityData activityData, CancellationToken cancellationToken = default);
}