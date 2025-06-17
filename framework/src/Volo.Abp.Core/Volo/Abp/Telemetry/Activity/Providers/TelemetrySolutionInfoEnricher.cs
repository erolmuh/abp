using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Telemetry.Activity.Contracts;
using Volo.Abp.Telemetry.Constants;

namespace Volo.Abp.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher))]
public class TelemetrySolutionInfoEnricher : ITelemetryActivityEventEnricher, IScopedDependency
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    public TelemetrySolutionInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    public bool IsFirstRun => true;
    public Type? DependsOn => typeof(TelemetrySessionInfoEnricher);

    public async Task<bool> CanExecuteAsync(ActivityContext context)
    {
        if (context.SolutionId.HasValue && !context.SolutionPath.IsNullOrEmpty())
        {
            return await _telemetryActivityStorage.ShouldAddSolutionInformation(context.SolutionId.Value);
        }

        return false;
    }

    public Task<Dictionary<string, object>?> EnrichAsync(ActivityContext context)
    {
        try
        {
            var result = new Dictionary<string, object>();

            var jsonContent = File.ReadAllText(context.SolutionPath!);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            AddSolutionCreationConfiguration(result, root.GetProperty("creatingStudioConfiguration"));

            if (root.TryGetProperty("modules", out var modulesElement) &&
                modulesElement.ValueKind == JsonValueKind.Object)
            {
                result[ActivityPropertyNames.InstalledModules] = ExtractModules(context.SolutionPath!, modulesElement);
            }

            result[ActivityPropertyNames.HasSolutionInfo] = true;

            return Task.FromResult<Dictionary<string, object>?>(result);
        }
        catch 
        {
            return Task.FromResult<Dictionary<string, object>?>(null);
        }
    }

   

    private void AddSolutionCreationConfiguration(Dictionary<string, object> result, JsonElement config)
    {
        result[ActivityPropertyNames.Template] = config.TryGetProperty("template", out var template) ? template.GetRawText() : string.Empty;
        result[ActivityPropertyNames.CreatedAbpStudioVersion] = config.TryGetProperty("createdAbpStudioVersion", out var createdAbpStudioVersion) ? createdAbpStudioVersion.GetRawText() : string.Empty;
        result[ActivityPropertyNames.MultiTenancy] = config.TryGetProperty("multiTenancy", out var multiTenancy) && ParseBool(multiTenancy);
        result[ActivityPropertyNames.UiFramework] = config.TryGetProperty("uiFramework", out var uiFramework) ? uiFramework.GetRawText() : string.Empty;
        result[ActivityPropertyNames.DatabaseProvider] = config.TryGetProperty("databaseProvider", out var databaseProvider) ? databaseProvider.GetRawText() : string.Empty;
        result[ActivityPropertyNames.Theme] = config.TryGetProperty("theme", out var theme) ? theme.GetRawText() : string.Empty;
        result[ActivityPropertyNames.ThemeStyle] = config.TryGetProperty("themeStyle", out var themeStyle) ? themeStyle.GetRawText() : string.Empty;
        result[ActivityPropertyNames.HasPublicWebsite] = config.TryGetProperty("publicWebsite", out var publicWebsite) && ParseBool(publicWebsite);
        result[ActivityPropertyNames.IsTiered] = config.TryGetProperty("tiered", out var tiered) && ParseBool(tiered);
        result[ActivityPropertyNames.SocialLogins] = config.TryGetProperty("socialLogin", out var socialLogin) && ParseBool(socialLogin);
        result[ActivityPropertyNames.DatabaseManagementSystem] = config.TryGetProperty("databaseManagementSystem", out var databaseManagementSystem) ? databaseManagementSystem.GetRawText() : string.Empty;
        result[ActivityPropertyNames.IsSeparateTenantSchema] = config.TryGetProperty("separateTenantSchema", out var separateTenantSchema) && ParseBool(separateTenantSchema);
        result[ActivityPropertyNames.MobileFramework] = config.TryGetProperty("mobileFramework", out var mobileFramework) ? mobileFramework.GetRawText() : string.Empty;
        result[ActivityPropertyNames.IncludeTests] = config.TryGetProperty("includeTests", out var includeTests) && ParseBool(includeTests);
        result[ActivityPropertyNames.DynamicLocalization] =  config.TryGetProperty("dynamicLocalization", out var dynamicLocalization) && ParseBool(dynamicLocalization);
        result[ActivityPropertyNames.KubernetesConfiguration] = config.TryGetProperty("kubernetesConfiguration", out var kubernetesConfiguration) && ParseBool(kubernetesConfiguration);
        result[ActivityPropertyNames.GrafanaDashboard] =  config.TryGetProperty("grafanaDashboard", out var grafanaDashboard) && ParseBool(grafanaDashboard);
    }

    private List<Dictionary<string, object>> ExtractModules(string solutionPath, JsonElement modulesElement)
    {
        var modules = new List<Dictionary<string, object>>();

        foreach (var module in modulesElement.EnumerateObject())
        {
            var modulePath = GetModuleFilePath(solutionPath, module);
            if (modulePath.IsNullOrEmpty())
            {
                continue;
            }

            var moduleJson = File.ReadAllText(modulePath);
            using var moduleDoc = JsonDocument.Parse(moduleJson);

            if (!moduleDoc.RootElement.TryGetProperty("imports", out var imports) ||
                imports.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var import in imports.EnumerateObject())
            {
                var version = import.Value.GetProperty("version").GetString();

                var creationTime = import.Value.TryGetProperty("creationTime", out var ct)
                    ? DateTimeOffset.Parse(ct.GetString()!) // TODO: tryParse?
                    : (DateTimeOffset?)null;

                if (modules.Any(x =>
                        x.TryGetValue(ActivityPropertyNames.ModuleName, out var n) && n as string == import.Name &&
                        x.TryGetValue(ActivityPropertyNames.ModuleVersion, out var v) && v as string == version))
                {
                    continue;
                }

                var moduleEntry = new Dictionary<string, object> { { ActivityPropertyNames.ModuleName, import.Name } };
                if (!version.IsNullOrEmpty())
                {
                    moduleEntry[ActivityPropertyNames.ModuleVersion] = version;
                }

                if (creationTime.HasValue)
                {
                    moduleEntry[ActivityPropertyNames.ModuleInstallationTime] = creationTime.Value;
                }

                modules.Add(moduleEntry);
            }
        }

        return modules;
    }

    private string? GetModuleFilePath(string solutionPath, JsonProperty module)
    {
        if (!module.Value.TryGetProperty("path", out var pathElement))
        {
            return null;
        }

        var path = pathElement.GetString();
        if (path.IsNullOrEmpty())
        {
            return null;
        }

        var fullPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, path);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private bool ParseBool(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var b) => b,
            _ => false
        };
    }
}