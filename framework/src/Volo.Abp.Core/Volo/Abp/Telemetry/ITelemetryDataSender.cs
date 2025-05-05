using System.Threading.Tasks;

namespace Volo.Abp.Telemetry;

public interface ITelemetryDataSender
{
    Task SendAsync();
}