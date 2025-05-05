using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Contracts;

internal interface ISoftwareDetector : IScopedDependency
{
    string Name { get; }
    Task<SoftwareInfo?> DetectAsync();
}