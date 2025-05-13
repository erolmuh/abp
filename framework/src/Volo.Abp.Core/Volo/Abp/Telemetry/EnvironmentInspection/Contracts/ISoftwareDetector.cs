using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

public interface ISoftwareDetector 
{
    string Name { get; }
    Task<SoftwareInfo?> DetectAsync();
}