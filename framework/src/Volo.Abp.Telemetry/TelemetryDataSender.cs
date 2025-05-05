using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Activity;
using Volo.Abp.Cli;
using Volo.Abp.Cli.Http;
using Volo.Abp.DependencyInjection;

public class TelemetryDataSender : ITelemetryDataSender , IScopedDependency
{
    private readonly CliHttpClientFactory _cliHttpClientFactory;
    private readonly IActivityStorage _activityStorage;
    private readonly IActivityDataProvider _activityDataProvider;

    public TelemetryDataSender(CliHttpClientFactory cliHttpClientFactory, IActivityStorage activityStorage, IActivityDataProvider activityDataProvider)
    {
        _cliHttpClientFactory = cliHttpClientFactory;
        _activityStorage = activityStorage;
        _activityDataProvider = activityDataProvider;
    }

    public async Task SendAsync()
    {
        using var client = _cliHttpClientFactory.CreateClient();
        client.AddAbpAuthenticationToken();

        var activities = await _activityStorage.GetBufferedActivitiesAsync();

        if (activities.Count > 0)
        {
            foreach (var activity in activities)
            {
                await _activityDataProvider.AddExtraInformationAsync(activity);
                await client.PostAsync(CliUrls.TelemetryAbpIo,
                    new StringContent(JsonSerializer.Serialize(activity), Encoding.UTF8,
                        MediaTypeNames.Application.Json));
              
            }

            await _activityStorage.MarkActivitiesAsSentAsync();
        }
    }
}