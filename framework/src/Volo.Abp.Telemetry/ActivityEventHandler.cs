using System;
using System.Threading.Tasks;
using Activity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

public class ActivityEventHandler : ILocalEventHandler<ActivityData>, ITransientDependency
{
    private readonly IActivityStorage _activityStorage;
    private readonly IActivityDataProvider _activityDataProvider;
    private readonly ITelemetryDataSender _telemetryDataSender;

    public ActivityEventHandler(IActivityDataProvider activityDataProvider, IActivityStorage activityStorage, ITelemetryDataSender telemetryDataSender)
    {
        _activityDataProvider = activityDataProvider;
        _activityStorage = activityStorage;
        _telemetryDataSender = telemetryDataSender;
    }

    public async Task HandleEventAsync(ActivityData eventData)
    {
        var build = await _activityDataProvider.AddExtraInformationAsync(eventData);
        await _activityStorage.BufferActivityAsync(build);

        await CheckIfActivitySendTimeIsDueAsync();
    }

    private async Task CheckIfActivitySendTimeIsDueAsync()
    {
        var lastActivitySendTime = await _activityStorage.GetLastActivitySendTimeAsync();
        if (lastActivitySendTime is null)
        {
            await _telemetryDataSender.SendAsync();
        }
        
        if (lastActivitySendTime is not null && lastActivitySendTime > DateTimeOffset.UtcNow.AddDays(7) )
        {
            await _telemetryDataSender.SendAsync();
        }
    }
}