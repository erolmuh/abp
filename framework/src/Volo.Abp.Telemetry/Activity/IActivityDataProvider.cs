using Volo.Abp.DependencyInjection;

namespace Volo.Abp.Telemetry.Activity;

public interface IActivityDataProvider : ITransientDependency
{
    Task<ActivityData> AddExtraInformationAsync(ActivityData activityData, CancellationToken cancellationToken = default);
}