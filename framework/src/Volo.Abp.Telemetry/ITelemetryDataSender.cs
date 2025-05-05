using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

public interface ITelemetryDataSender
{
    Task SendAsync();
}