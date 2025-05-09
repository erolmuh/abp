using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Shared;

namespace Volo.Abp.Telemetry;

public class TelemetryDataSender : ITelemetryDataSender, IScopedDependency
{
    private readonly IActivityStorage _activityStorage;
    private readonly IActivityDataProvider _activityDataProvider;
    private const int ActivityBatchSize = 50;

    public TelemetryDataSender(IActivityStorage activityStorage, IActivityDataProvider activityDataProvider)
    {
        _activityStorage = activityStorage;
        _activityDataProvider = activityDataProvider;
    }

    public async Task SendAsync()
    {
        using var httpClient = new HttpClient();
        AddAbpAuthenticationTokenAsync(httpClient);

        var activities = await _activityStorage.GetBufferedActivitiesAsync();

        if (activities.Count > 0)
        {
            for (var i = 0; i < activities.Count; i += ActivityBatchSize)
            {
                var activityBatch = activities.Skip(i).Take(ActivityBatchSize).ToList();

                foreach (var activity in activityBatch)
                {
                    await AddExtraInformationAsync(activity);
                }

                await httpClient.PostAsync($"{AbpPlatformUrls.TelemetryApiUrl}api/telemetry/collect",
                    new StringContent(JsonSerializer.Serialize(activityBatch), Encoding.UTF8, "application/json"));
            }

            await _activityStorage.MarkActivitiesAsSentAsync();
        }
    }

    private async Task AddExtraInformationAsync(ActivityData activityData)
    {
        try
        {
            var (isFirstSession, sessionId) = await _activityStorage.GetOrCreateSessionInfoAsync();

            activityData.Add(ActivityPropertyName.SessionId, sessionId);
            activityData.Add(ActivityPropertyName.IsFirstSession, isFirstSession);

            activityData.Add(ActivityPropertyName.DeviceId, await _activityDataProvider.ReadDeviceIdAsync());


            var lastDeviceInfoSendTime = await _activityStorage.GetLastDeviceInfoSendTimeAsync();

            if (lastDeviceInfoSendTime is null || DateTimeOffset.UtcNow - lastDeviceInfoSendTime > TimeSpan.FromDays(7))
            {
                await _activityDataProvider.AddDeviceInformationAsync(activityData);
                await _activityStorage.MarkDeviceInfoAsSentAsync();
            }

            if (activityData.ContainsKey(ActivityPropertyName.Assembly))
            {
                await _activityDataProvider.AddApplicationInformation(activityData);
            }

            if (activityData.TryGetValue(ActivityPropertyName.SolutionPath, out var path))
            {
                var id = _activityDataProvider.ReadSolutionId((string)path);
                if (id.HasValue)
                {
                    activityData.Add(ActivityPropertyName.SolutionId, id);
                    await _activityDataProvider.AddSolutionInformationAsync(activityData);
                }
            }
        }
        catch
        {
            //ignored
        }
    }

    private static void AddAbpAuthenticationTokenAsync(HttpClient httpClient)
    {
        if (!File.Exists(AbpTelemetryPaths.AccessToken))
        {
            return;
        }

        var accessToken = File.ReadAllText(AbpTelemetryPaths.AccessToken, Encoding.UTF8);
        if (!accessToken.IsNullOrEmpty())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}