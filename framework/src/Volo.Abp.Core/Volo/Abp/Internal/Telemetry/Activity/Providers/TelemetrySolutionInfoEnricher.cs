using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Internal.Telemetry.Activity.Contracts;
using Volo.Abp.Internal.Telemetry.Constants;

namespace Volo.Abp.Internal.Telemetry.Activity.Providers;

[ExposeServices(typeof(ITelemetryActivityEventEnricher), typeof(IHasParentTelemetryActivityEventEnricher))]
internal sealed class TelemetrySolutionInfoEnricher : TelemetryActivityEventEnricher, IHasParentTelemetryActivityEventEnricher
{
    private readonly ITelemetryActivityStorage _telemetryActivityStorage;

    public TelemetrySolutionInfoEnricher(ITelemetryActivityStorage telemetryActivityStorage,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _telemetryActivityStorage = telemetryActivityStorage;
    }

    public Type Parent => typeof(TelemetrySessionInfoEnricher);

    public override Task<bool> CanExecuteAsync(ActivityContext context)
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
        context.Current[ActivityPropertyNames.Template] = config.GetString("template");
        context.Current[ActivityPropertyNames.CreatedAbpStudioVersion] = config.GetString("createdAbpStudioVersion");
        context.Current[ActivityPropertyNames.MultiTenancy] = config.GetBoolean("multiTenancy");
        context.Current[ActivityPropertyNames.UiFramework] = config.GetString("uiFramework");
        context.Current[ActivityPropertyNames.DatabaseProvider] = config.GetString("databaseProvider");
        context.Current[ActivityPropertyNames.Theme] = config.GetString("theme");
        context.Current[ActivityPropertyNames.ThemeStyle] = config.GetString("themeStyle");
        context.Current[ActivityPropertyNames.HasPublicWebsite] = config.GetBoolean("publicWebsite");
        context.Current[ActivityPropertyNames.IsTiered] = config.GetBoolean("tiered");
        context.Current[ActivityPropertyNames.SocialLogins] = config.GetBoolean("socialLogin");
        context.Current[ActivityPropertyNames.DatabaseManagementSystem] = config.GetString("databaseManagementSystem");
        context.Current[ActivityPropertyNames.IsSeparateTenantSchema] = config.GetBoolean("separateTenantSchema");
        context.Current[ActivityPropertyNames.MobileFramework] = config.GetString("mobileFramework");
        context.Current[ActivityPropertyNames.IncludeTests] = config.GetBoolean("includeTests");
        context.Current[ActivityPropertyNames.DynamicLocalization] = config.GetBoolean("dynamicLocalization");
        context.Current[ActivityPropertyNames.KubernetesConfiguration] = config.GetBoolean("kubernetesConfiguration");
        context.Current[ActivityPropertyNames.GrafanaDashboard] = config.GetBoolean("grafanaDashboard");
    }

    private static void AddModuleInfo(ActivityContext context, JsonElement modulesElement)
    {
        var modules = new List<Dictionary<string, object>>();

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
                modules.Add(new Dictionary<string, object>
                {
                    { ActivityPropertyNames.ModuleName, import.Name },
                    { ActivityPropertyNames.ModuleVersion, import.Value.GetString("version") },
                    { ActivityPropertyNames.ModuleInstallationTime, import.Value.TryGetDateTimeOffset("creationTime", out var creationTime) ? creationTime :  DateTimeOffset.MinValue }
                });
            }
        }

        context.Current[ActivityPropertyNames.InstalledModules] = modules;
    }

    private static string? GetModuleFilePath(string solutionPath, JsonProperty module)
    {
        var path = module.Value.GetString("path");
        if (path.IsNullOrEmpty())
        {
            return null;
        }

        var fullPath = Path.Combine(Path.GetDirectoryName(solutionPath)!, path);
        return File.Exists(fullPath) ? fullPath : null;
    }
}