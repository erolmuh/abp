using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;
using Volo.Abp.Internal.Telemetry.Constants;

namespace Volo.Abp.Internal.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher), typeof(IHasParentTelemetryActivityEventEnricher<TelemetrySessionInfoEnricher>))]
internal sealed class TelemetrySolutionInfoEnricher : TelemetryActivityEventEnricher, IHasParentTelemetryActivityEventEnricher<TelemetrySessionInfoEnricher>
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    public TelemetrySolutionInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    protected override Task<bool> CanExecuteAsync(ActivityContext context)
    {
        if (context.SolutionId.HasValue && !context.SolutionPath.IsNullOrEmpty())
        {
            return Task.FromResult(_telemetryActivityStorage.ShouldAddSolutionInformation(context.SolutionId.Value));
        }

        return Task.FromResult(false);
    }

    protected override Task ExecuteAsync(ActivityContext context)
    {
        try
        {
            var jsonContent = File.ReadAllText(context.SolutionPath!);
            using var doc = JsonDocument.Parse(jsonContent);

            var root = doc.RootElement;

            if (root.TryGetProperty("creatingStudioConfiguration", out var creatingStudioConfiguration))
            {
                AddSolutionCreationConfiguration(context, creatingStudioConfiguration);
            }

            if (root.TryGetProperty("modules", out var modulesElement))
            {
                AddModuleInfo(context, modulesElement);
            }

            context.Current[ActivityPropertyNames.HasSolutionInfo] = true;
        }
        catch
        {
            //ignored
        }

        return Task.CompletedTask;
    }

    private static void AddSolutionCreationConfiguration(ActivityContext context, JsonElement config)
    {
        context.Current[ActivityPropertyNames.Template] = TelemetryJsonElementExtensions.GetStringOrNull(config, "template");
        context.Current[ActivityPropertyNames.CreatedAbpStudioVersion] = TelemetryJsonElementExtensions.GetStringOrNull(config,"createdAbpStudioVersion");
        context.Current[ActivityPropertyNames.MultiTenancy] =  TelemetryJsonElementExtensions.GetBooleanOrNull(config,"multiTenancy");
        context.Current[ActivityPropertyNames.UiFramework] = TelemetryJsonElementExtensions.GetStringOrNull(config,"uiFramework");
        context.Current[ActivityPropertyNames.DatabaseProvider] = TelemetryJsonElementExtensions.GetStringOrNull(config,"databaseProvider");
        context.Current[ActivityPropertyNames.Theme] = TelemetryJsonElementExtensions.GetStringOrNull(config,"theme");
        context.Current[ActivityPropertyNames.ThemeStyle] = TelemetryJsonElementExtensions.GetStringOrNull(config,"themeStyle"); 
        context.Current[ActivityPropertyNames.HasPublicWebsite] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"publicWebsite");
        context.Current[ActivityPropertyNames.IsTiered] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"tiered");
        context.Current[ActivityPropertyNames.SocialLogins] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"socialLogin");
        context.Current[ActivityPropertyNames.DatabaseManagementSystem] = TelemetryJsonElementExtensions.GetStringOrNull(config,"databaseManagementSystem");
        context.Current[ActivityPropertyNames.IsSeparateTenantSchema] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"separateTenantSchema");
        context.Current[ActivityPropertyNames.MobileFramework] = TelemetryJsonElementExtensions.GetStringOrNull(config,"mobileFramework"); 
        context.Current[ActivityPropertyNames.IncludeTests] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"includeTests"); 
        context.Current[ActivityPropertyNames.DynamicLocalization] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"dynamicLocalization");
        context.Current[ActivityPropertyNames.KubernetesConfiguration] = TelemetryJsonElementExtensions.GetBooleanOrNull(config,"kubernetesConfiguration");
        context.Current[ActivityPropertyNames.GrafanaDashboard] =  TelemetryJsonElementExtensions.GetBooleanOrNull(config,"grafanaDashboard");
    }

    private static void AddModuleInfo(ActivityContext context, JsonElement modulesElement)
    {
        var modules = new List<Dictionary<string, object?>>();

        foreach (var module in modulesElement.EnumerateObject())
        {
            var modulePath = GetModuleFilePath(context.SolutionPath!, module);
            if (modulePath.IsNullOrEmpty())
            {
                continue;
            }

            var moduleJsonFileContent = File.ReadAllText(modulePath);
            using var moduleDoc = JsonDocument.Parse(moduleJsonFileContent);

            if (!moduleDoc.RootElement.TryGetProperty("imports", out var imports))
            {
                continue;
            }

            foreach (var import in imports.EnumerateObject())
            {
                modules.Add(new Dictionary<string, object?>
                {
                    { ActivityPropertyNames.ModuleName, import.Name },
                    { ActivityPropertyNames.ModuleVersion, TelemetryJsonElementExtensions.GetStringOrNull(import.Value,"version") },
                    { ActivityPropertyNames.ModuleInstallationTime, TelemetryJsonElementExtensions.GetDateTimeOffsetOrNull(import.Value, "creationTime") }
                });
            }
        }

        context.Current[ActivityPropertyNames.InstalledModules] = modules;
    }

    private static string? GetModuleFilePath(string solutionPath, JsonProperty module)
    {
        var path = TelemetryJsonElementExtensions.GetStringOrNull(module.Value,"path");
        if (path.IsNullOrEmpty())
        {
            return null;
        }

        var fullPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, path);
        return File.Exists(fullPath) ? fullPath : null;
    }
}