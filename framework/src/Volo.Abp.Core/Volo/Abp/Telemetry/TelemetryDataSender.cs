using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Shared;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;

namespace Volo.Abp.Telemetry;

public class TelemetryDataSender : ITelemetryDataSender , IScopedDependency
{

#if DEBUG
    private const string ApiUrl = "https://localhost:44393/api/telemetry/collect";
#else
    private const string ApiUrl = "https://telemetry.abp.io/api/telemetry/collect";
#endif
    private readonly IActivityStorage _activityStorage;
    private readonly IActivityDataProvider _activityDataProvider;

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
            await AddExtraInformationAsync(activities[0]);
            foreach (var activity in activities)
            {
                await httpClient.PostAsync(ApiUrl,
                    new StringContent(JsonSerializer.Serialize(activity), Encoding.UTF8, "application/json"));
            }

            await _activityStorage.MarkActivitiesAsSentAsync();
        }
    }

    private async Task AddExtraInformationAsync(ActivityData activityData)
    {
        try
        {
            var (isFirstSession, sessionId) = await _activityStorage.GetOrCreateSessionInfoAsync();

            activityData.Add(ActivityPropertyNameConstants.SessionId, sessionId);
            activityData.Add(ActivityPropertyNameConstants.IsFirstSession, isFirstSession);

            var lastDeviceInfoSendTime = await _activityStorage.GetLastDeviceInfoSendTimeAsync();

            if (lastDeviceInfoSendTime is null || DateTimeOffset.UtcNow - lastDeviceInfoSendTime > TimeSpan.FromDays(7))
            {
                await _activityDataProvider.AddDeviceInformationAsync(activityData);
                await _activityStorage.MarkDeviceInfoAsSentAsync();
                
            }

            if (activityData.TryGetValue(ActivityPropertyNameConstants.Assembly, out var assemblyLocation))
            {
                _activityDataProvider.AddApplicationInformation(activityData, Assembly.LoadFrom((string) assemblyLocation));
            }

            if (activityData.TryGetValue(ActivityPropertyNameConstants.SolutionPath, out var path))
            {
                var solutionPath = path as string;
                if (string.IsNullOrEmpty(solutionPath) || !File.Exists(solutionPath))
                {
                    return;
                }

                await _activityDataProvider.AddSolutionInformationAsync(activityData);
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