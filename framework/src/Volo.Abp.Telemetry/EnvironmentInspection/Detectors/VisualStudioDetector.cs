using System.Threading.Tasks;
using EnvironmentInspection.Contracts;

namespace EnvironmentInspection.Detectors;

internal class VisualStudioDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Visual Studio";

    public async override  Task<SoftwareInfo?> DetectAsync()
    {
        return null;
    }
}