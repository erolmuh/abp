using System.Threading.Tasks;
using EnvironmentInspection.Contracts;
using EnvironmentInspection.Enums;
using System;

namespace EnvironmentInspection.Detectors;

internal class NodeJsDetector : SoftwareDetector, ISoftwareDetector
{
    public override string Name => "Node.js";

    public async override Task<SoftwareInfo?> DetectAsync()
    {
        try
        {
            var output = await ExecuteCommandAsync("node", "-v");

            if (output.IsNullOrWhiteSpace())
            {
                return null;
            }

            var version = output.Trim().TrimStart('v');

            return new SoftwareInfo(Name, version, uiTheme: null, SoftwareType.Others);
        }
        catch
        {
            return null;
        }
    }
}