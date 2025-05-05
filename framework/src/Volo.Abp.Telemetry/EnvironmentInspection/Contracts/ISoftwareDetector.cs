using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace EnvironmentInspection.Contracts;

internal interface ISoftwareDetector : IScopedDependency
{
    string Name { get; }
    Task<SoftwareInfo?> DetectAsync();
}