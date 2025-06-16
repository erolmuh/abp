using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;
using Volo.Abp.Telemetry.Constants.Enums;
using Volo.Abp.Telemetry.Helpers;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetryApplicationInfoEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    public TelemetryApplicationInfoEnricher(
        ITelemetryActivityStorage telemetryActivityStorage)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    public bool IsFirstRun => true;
    public Type DependsOn => typeof(TelemetrySessionInfoEnricher);
    
    public Task<bool> CanExecuteAsync(ActivityContext context)
    {
        return Task.FromResult(context.SessionType == SessionType.ApplicationRuntime);
    }

    public async Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is null)
            {
                context.Terminate();
                return null;
            }

            var projectMetaData = AbpProjectMetadataReader.ReadProjectMetadata(entryAssembly);
            if (projectMetaData?.ProjectId == null || projectMetaData.AbpSlnPath.IsNullOrEmpty())
            {
                context.Terminate();
                return null;
            }

            if (!await _telemetryActivityStorage.ShouldAddProjectInfoAsync(projectMetaData.ProjectId.Value))
            {
                context.Cancel();
                return null;
            }

            context.ExtraProperties[ActivityPropertyNames.SolutionPath] = projectMetaData.AbpSlnPath;
            
            var solutionId = ReadSolutionIdFromSolutionPath(projectMetaData.AbpSlnPath);
            
            if (!solutionId.HasValue)
            {
                context.Terminate();
                return null;
            }
            
            var result = new Dictionary<string, object>
            {
                { ActivityPropertyNames.SolutionId, solutionId },
                { ActivityPropertyNames.ProjectId, projectMetaData.ProjectId.Value },
                { ActivityPropertyNames.ProjectType, projectMetaData.Role ?? string.Empty },
            };

            result[ActivityPropertyNames.HasProjectInfo] = true;
            return result;
            
        }
        catch
        {
            return null;
        }
    }

    private static Guid? ReadSolutionIdFromSolutionPath(string solutionPath)
    {
        try
        {
            if (solutionPath.IsNullOrEmpty())
            {
                return null;
            }
            using var fs = new FileStream(solutionPath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var doc = JsonDocument.Parse(fs);
            if (doc.RootElement.TryGetProperty("id", out var id) &&
                Guid.TryParse(id.GetString(), out var solutionId))
            {
                return solutionId;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }
}