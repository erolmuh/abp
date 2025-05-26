using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Shared;

namespace Volo.Abp.Telemetry;

public class TelemetryDataSender : ITelemetryDataSender, IScopedDependency
{
    private readonly IActivityStorage _activityStorage;
    private const int ActivityBatchSize = 50;

    public TelemetryDataSender(IActivityStorage activityStorage)
    {
        _activityStorage = activityStorage;
    }

    public async Task SendAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            AddAbpAuthenticationTokenAsync(httpClient);

            var activities = await _activityStorage.GetBufferedActivitiesAsync();
            if (activities.Count == 0)
            {
                return;
            }


            for (var i = 0; i < activities.Count; i += ActivityBatchSize)
            {
                var activityBatch = activities.Skip(i).Take(ActivityBatchSize).ToList();

                await httpClient.PostAsync($"{AbpPlatformUrls.TelemetryApiUrl}api/telemetry/collect",
                    new StringContent(JsonSerializer.Serialize(activityBatch), Encoding.UTF8, "application/json"));

            }
            await _activityStorage.MarkActivitiesAsSentAsync();
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