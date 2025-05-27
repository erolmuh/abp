using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class TelemetryActivityDataProvider : ITelemetryActivityDataProvider, ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider;

    public TelemetryActivityDataProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public virtual async Task AddExtraInformationAsync(ActivityData activity)
    {
        var activityStorage = _serviceProvider.GetRequiredService<ITelemetryActivityStorage>();
        var sessionContext = _serviceProvider.GetRequiredService<ITelemetrySessionContextProvider>();
        var deviceInfoProvider = _serviceProvider.GetRequiredService<IDeviceInfoProvider>();
        
        var (isFirstSession, sessionId) = await activityStorage.GetOrCreateSessionInfoAsync();

        sessionContext.SetSolutionContext(activity);
        activity[ActivityPropertyName.SessionType] = sessionContext.SessionType;
        activity[ActivityPropertyName.SessionId] = sessionId;
        activity[ActivityPropertyName.IsFirstSession] = isFirstSession;
        activity[ActivityPropertyName.DeviceId] = deviceInfoProvider.GetDeviceId();

        if (await activityStorage.ShouldAddDeviceInfoAsync())
        {
            activity[ActivityPropertyName.DeviceType] = deviceInfoProvider.GetDeviceType();
            activity[ActivityPropertyName.DeviceLanguage] = deviceInfoProvider.GetLanguage();
            activity[ActivityPropertyName.OperatingSystem] = deviceInfoProvider.GetOperatingSystem();
            activity[ActivityPropertyName.CountryIsoCode] = deviceInfoProvider.GetCountry();

            var softwareInfo = _serviceProvider.GetRequiredService<ISoftwareInfoProvider>();
            var softwareList = await softwareInfo.GetSoftwareInfoAsync();
            activity[ActivityPropertyName.InstalledSoftwares] = softwareList;

            await activityStorage.MarkDeviceInfoAsSentAsync();
        }

        if (activity.ContainsKey(ActivityPropertyName.Assembly) && sessionContext.SessionType == SessionType.ApplicationRuntime)
        {
            var contributors = _serviceProvider.GetRequiredService<IEnumerable<ITelemetryApplicationInfoContributor>>();
            foreach (var contributor in contributors)
            {
                await contributor.ContributeAsync(activity);
            }
            activity.Remove(ActivityPropertyName.Assembly);
        }

        if (activity.TryGetValue(ActivityPropertyName.SolutionId, out var rawSolutionId) && Guid.TryParse(rawSolutionId?.ToString(), out var solutionId))
        {

            if (await activityStorage.ShouldAddSolutionInformation(solutionId))
            {
                var solutionProvider = _serviceProvider.GetRequiredService<ITelemetrySolutionInfoProvider>();
                var solutionInfo = await solutionProvider.GetSolutionInfoAsync(activity[ActivityPropertyName.SolutionPath].ToString()!);

                foreach (var entry in solutionInfo)
                {
                    activity[entry.Key] = entry.Value;
                }
            }
        }
    }

}