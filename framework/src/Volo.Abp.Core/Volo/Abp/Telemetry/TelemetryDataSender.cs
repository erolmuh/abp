using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry;

public class TelemetryDataSender : ITelemetryDataSender, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private const int ActivityBatchSize = 50;

    public TelemetryDataSender(ITelemetryActivityStorage telemetryActivityStorage)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    public async Task SendAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            AddAbpAuthenticationTokenAsync(httpClient);

            var activities = await _telemetryActivityStorage.GetBufferedActivitiesAsync();
            if (activities.Count == 0)
            {
                return;
            }


            for (var i = 0; i < activities.Count; i += ActivityBatchSize)
            {
                var activityBatch = activities.Skip(i).Take(ActivityBatchSize).ToList();

                await httpClient.PostAsync($"{AbpPlatformUrls.AbpTelemetryApiUrl}api/telemetry/collect",
                    new StringContent(JsonSerializer.Serialize(activityBatch), Encoding.UTF8, "application/json"));

            }
            await _telemetryActivityStorage.MarkActivitiesAsSentAsync();
        }
        catch
        {
            //ignored
        }
    }


    private static void AddAbpAuthenticationTokenAsync(HttpClient httpClient)
    {
        if (!File.Exists(TelemetryPaths.AccessToken))
        {
            return;
        }

        var accessToken = File.ReadAllText(TelemetryPaths.AccessToken, Encoding.UTF8);
        if (!accessToken.IsNullOrEmpty())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}