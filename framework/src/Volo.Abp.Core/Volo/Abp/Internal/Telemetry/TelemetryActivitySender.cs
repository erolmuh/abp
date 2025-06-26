using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Internal.Telemetry.Activity;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;
using Volo.Abp.Internal.Telemetry.Constants;

namespace Volo.Abp.Internal.Telemetry;

public class TelemetryActivitySender : ITelemetryActivitySender, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;
    private readonly ILogger<TelemetryActivitySender> _logger;

    private const int ActivitySendBatchSize = 50;

    public TelemetryActivitySender(ITelemetryActivityStorage telemetryActivityStorage, ILogger<TelemetryActivitySender> logger)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
        _logger = logger;
    }

    private async Task SendAsync()
    {
        try
        {
            var activities = _telemetryActivityStorage.GetActivities();
            var activityBatches = ChunkActivities(activities);
            
            using var httpClient = new HttpClient();
            AddJwtTokenIfAuthenticated(httpClient);

            foreach (var activityBatch in activityBatches)
            {
                try
                {
                    var response = await httpClient.PostAsync(
                        $"{AbpPlatformUrls.AbpTelemetryApiUrl}api/telemetry/collect",
                        new StringContent(JsonSerializer.Serialize(activityBatch), Encoding.UTF8,
                            "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        _telemetryActivityStorage.DeleteActivities(activityBatch);
                    }
                    else
                    {
                        _logger.LogWithLevel(LogLevel.Trace,
                            $"Failed to send telemetry activities. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                        
                        _telemetryActivityStorage.MarkActivitiesAsFailed(activityBatch);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWithLevel(LogLevel.Trace,
                        $"Error while sending telemetry activities: {ex.Message}");

                    return;
                }
            }
        }
        catch
        {
            //ignored
        }
    }

    public async Task TrySendQueuedActivitiesAsync()
    {
        if (!_telemetryActivityStorage.ShouldSendActivities())
        {
            return;
        }

        await SendAsync();
    }

    private static IEnumerable<ActivityEvent[]> ChunkActivities(List<ActivityEvent> activities)
    {
        return activities
            .Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / ActivitySendBatchSize)
            .Select(x => x.Select(v => v.Value).ToArray());
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