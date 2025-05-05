using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
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
            foreach (var activity in activities)
            {
                await _activityDataProvider.AddExtraInformationAsync(activity);
                await httpClient.PostAsync(ApiUrl,
                    new StringContent(JsonSerializer.Serialize(activity), Encoding.UTF8, "application/json"));
              
            }

            await _activityStorage.MarkActivitiesAsSentAsync();
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