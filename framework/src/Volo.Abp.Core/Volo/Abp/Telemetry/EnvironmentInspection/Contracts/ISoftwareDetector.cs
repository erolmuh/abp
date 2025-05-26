using System.Threading.Tasks;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

internal interface ISoftwareDetector 
{
    string Name { get; }
    Task<SoftwareInfo?> DetectAsync();
}