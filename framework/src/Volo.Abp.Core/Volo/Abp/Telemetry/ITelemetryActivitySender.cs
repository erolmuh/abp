using System.Threading.Tasks;

namespace Volo.Abp.Telemetry;

public interface ITelemetryActivitySender
{
    Task SendAsync();
}