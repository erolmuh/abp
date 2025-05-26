using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using Volo.Abp.Telemetry.Shared;
using Volo.Abp.Telemetry.Shared.Enums;

namespace Volo.Abp.Telemetry.Activity.Providers;

public class ActivityDataProvider : IActivityDataProvider, IScopedDependency
{
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly ISoftwareInfoProvider _softwareInfoProvider;
    private readonly IEnumerable<ITelemetryApplicationInfoContributor> _applicationInfoContributors;
    private readonly IActivityStorage _activityStorage;
    private readonly ISolutionInfoProvider _solutionInfoProvider;

    public ActivityDataProvider(
        IDeviceInfoProvider deviceInfoProvider,
        ISoftwareInfoProvider softwareInfoProvider,
        IEnumerable<ITelemetryApplicationInfoContributor> applicationInfoContributors,
        IActivityStorage activityStorage, ISolutionInfoProvider solutionInfoProvider)
    {
        _deviceInfoProvider = deviceInfoProvider;
        _softwareInfoProvider = softwareInfoProvider;
        _applicationInfoContributors = applicationInfoContributors;
        _activityStorage = activityStorage;
        _solutionInfoProvider = solutionInfoProvider;
    }

    protected virtual SessionType GetSessionType()
    {
        return SessionType.ApplicationRuntime;
    }

    public virtual async Task AddExtraInformationAsync(ActivityData activity)
    {
        var (isFirstSession, sessionId) = await _activityStorage.GetOrCreateSessionInfoAsync();

        AddSolutionId(activity);
        var sessionType = GetSessionType();
        activity[ActivityPropertyName.SessionType] = sessionType;
        activity[ActivityPropertyName.SessionId] = sessionId;
        activity[ActivityPropertyName.IsFirstSession] = isFirstSession;
        activity[ActivityPropertyName.DeviceId] = _deviceInfoProvider.GetDeviceId();

        if (await _activityStorage.ShouldAddDeviceInfoAsync())
        {
            await AddDeviceInformationAsync(activity);
        }

        if (activity.ContainsKey(ActivityPropertyName.Assembly) && sessionType == SessionType.ApplicationRuntime)
        {
            await AddApplicationInformationAsync(activity);
            activity.Remove(ActivityPropertyName.Assembly);
        }

        if (await ShouldAddSolutionInformation(activity))
        {
            var solutionInfo = await _solutionInfoProvider.GetSolutionInfoAsync(activity[ActivityPropertyName.SolutionPath].ToString()!);
            
            foreach (var entry in solutionInfo)
            {
                activity[entry.Key] = entry.Value;
            }
        }
    }

    protected virtual async Task<bool> ShouldAddSolutionInformation(ActivityData activity)
    {

        if (
            !activity.TryGetValue(ActivityPropertyName.SolutionId, out var id) ||
            !Guid.TryParse(id.ToString(), out var solutionId) && activity.ContainsKey(ActivityPropertyName.SolutionPath))
        {
            return false;
        }

        return await _activityStorage.ShouldAddSolutionInformation(solutionId);
    }

    protected virtual async Task AddDeviceInformationAsync(ActivityData activityData)
    {
        activityData[ActivityPropertyName.DeviceType] = _deviceInfoProvider.GetDeviceType();
        activityData[ActivityPropertyName.DeviceLanguage] = _deviceInfoProvider.GetLanguage();
        activityData[ActivityPropertyName.OperatingSystem] = _deviceInfoProvider.GetOperatingSystem();
        activityData[ActivityPropertyName.CountryIsoCode] = _deviceInfoProvider.GetCountry();

        var softwareList = await _softwareInfoProvider.GetSoftwareInfoAsync();
        activityData[ActivityPropertyName.InstalledSoftwares] = softwareList;

        await _activityStorage.MarkDeviceInfoAsSentAsync();
    }

    protected virtual async Task AddApplicationInformationAsync(ActivityData activityData)
    {
        foreach (var contributor in _applicationInfoContributors)
        {
            await contributor.ContributeAsync(activityData);
        }
    }


    protected virtual void AddSolutionId(ActivityData activityData)
    {
        if (activityData.ContainsKey(ActivityPropertyName.SolutionId))
        {
            return;
        }

        if (!activityData.TryGetValue(ActivityPropertyName.SolutionPath, out var path) || !File.Exists((string)path))
        {
            return;
        }

        var solutionId = _solutionInfoProvider.GetSolutionId((string)path);
        
        if (solutionId.HasValue)
        {
            activityData[ActivityPropertyName.SolutionId] = solutionId.Value;
        }
    }
}
