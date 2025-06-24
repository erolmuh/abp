using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;
using Volo.Abp.Internal.Telemetry.Constants;

namespace Volo.Abp.Internal.Telemetry;

public class TelemetryActivitySender : ITelemetryActivitySender, ISingletonDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    private const int ActivitySendBatchSize = 50;

    public TelemetryActivitySender(ITelemetryActivityStorage telemetryActivityStorage)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    public async Task SendAsync() //TODO: Private?
    {
        try
        {
            var activities = _telemetryActivityStorage.GetActivities();

            using var httpClient = new HttpClient();
            AddJwtTokenIfAuthenticated(httpClient);

            for (var i = 0; i < activities.Count; i += ActivitySendBatchSize)
            {
                var activityBatch = activities.Skip(i).Take(ActivitySendBatchSize).ToArray();

                try //TODO: Discard try-catch to not retry if we get network-like exception right now
                {
                    var response = await httpClient.PostAsync(
                        $"{AbpPlatformUrls.AbpTelemetryApiUrl}api/telemetry/collect",
                        new StringContent(JsonSerializer.Serialize(activityBatch), Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        // TODO: Log Status Code and Message
                        _telemetryActivityStorage.DeleteAcitivities(/*activityBatch*/); //TODO: Bug: Should only work for succeed activities
                    }
                    else
                    {
                        _telemetryActivityStorage.MarkActivitiesAsFailed(activityBatch);
                    }
                }
                catch
                {
                    // TODO: Log
                    _telemetryActivityStorage.MarkActivitiesAsFailed(activityBatch);
                }
            }
        }
        catch
        {
            //ignored
        }
    }

    public async Task SendIfNeededAsync()
    {
        if (!_telemetryActivityStorage.ShouldSendActivities())
        {
            return;
        }

        await SendAsync();
    }


    private static void AddJwtTokenIfAuthenticated(HttpClient httpClient)
    {
        if (!File.Exists(TelemetryPaths.AccessToken))
        {
            return;
        }

        var accessToken = File.ReadAllText(TelemetryPaths.AccessToken, Encoding.UTF8);
        if (accessToken.IsNullOrEmpty())
        {
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}