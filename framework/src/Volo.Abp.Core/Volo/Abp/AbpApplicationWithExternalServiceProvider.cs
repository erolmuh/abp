using System;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Telemetry;
using Volo.Abp.Telemetry.Activity;
using Volo.Abp.Telemetry.Helpers;

namespace Volo.Abp;

internal class AbpApplicationWithExternalServiceProvider : AbpApplicationBase, IAbpApplicationWithExternalServiceProvider
{
    public AbpApplicationWithExternalServiceProvider(
        [NotNull] Type startupModuleType,
        [NotNull] IServiceCollection services,
        Action<AbpApplicationCreationOptions>? optionsAction
        ) : base(
            startupModuleType,
            services,
            optionsAction)
    {
        services.AddSingleton<IAbpApplicationWithExternalServiceProvider>(this);
    }

    void IAbpApplicationWithExternalServiceProvider.SetServiceProvider([NotNull] IServiceProvider serviceProvider)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ServiceProvider != null)
        {
            if (ServiceProvider != serviceProvider)
            {
                throw new AbpException("Service provider was already set before to another service provider instance.");
            }

            return;
        }

        SetServiceProvider(serviceProvider);
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        SetServiceProvider(serviceProvider);

        await InitializeModulesAsync();

        ConfigureTelemetry(serviceProvider);
    }

    public void Initialize([NotNull] IServiceProvider serviceProvider)
    {
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        SetServiceProvider(serviceProvider);

        InitializeModules();
        
        ConfigureTelemetry(serviceProvider);
        
    }
    private void ConfigureTelemetry(IServiceProvider serviceProvider)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                if (configuration.GetValue<bool>("Abp:Telemetry:Disable"))
                {
                    return;
                }

                var assembly = Assembly.GetEntryAssembly()!;

                var packageMetadata = AbpPackageMetadataHelper.GetMetaData(assembly);

                if (packageMetadata != null)
                {
                    var telemetryService = scope.ServiceProvider.GetRequiredService<ITelemetryService>();

                    await using var _ = telemetryService.TrackActivity(ActivityNameConsts.ApplicationRun, activity =>
                    {
                        activity[ActivityPropertyName.Assembly] = assembly.Location;
                        activity[ActivityPropertyName.ProjectId] = packageMetadata.ProjectId!;
                        activity[ActivityPropertyName.ProjectType] = packageMetadata.Role!;
                        activity[ActivityPropertyName.SolutionPath] = packageMetadata.AbpSlnPath!;
                    });

                }
            }
            catch (Exception e)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AbpApplicationWithExternalServiceProvider>>();
                logger.LogError(e, "An error occurred while configuring telemetry.");
            }
        });
    }
    public override void Dispose()
    {
        base.Dispose();

        if (ServiceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }
}
