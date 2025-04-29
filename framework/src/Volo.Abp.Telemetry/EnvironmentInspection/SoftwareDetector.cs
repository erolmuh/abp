using System.Diagnostics;
using Volo.Abp.Telemetry.EnvironmentInspection.Contracts;
using CliWrap;
using CliWrap.Buffered;

namespace Volo.Abp.Telemetry.EnvironmentInspection;

abstract internal class SoftwareDetector : ISoftwareDetector
{
    public abstract string Name { get; }
    public abstract Task<SoftwareInfo?> DetectAsync();

    protected async Task<string?> ExecuteCommandAsync(string command, string? arg)
    {
        var result = await CliWrap.Cli.Wrap(command)
            .WithArguments(arg) 
            .WithValidation(CommandResultValidation.None) 
            .ExecuteBufferedAsync();

        return result.StandardOutput.IsNullOrEmpty() ? null : result.StandardOutput.Trim();
    }
    
    protected static string? GetFileVersion(string filePath)
    {
        try
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
            return versionInfo.FileVersion;
        }
        catch
        {
            return string.Empty;
        }
    }
}