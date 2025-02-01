using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Volo.Abp.Autofac;

namespace Volo.Abp;

public static class AbpAutofacAbpApplicationCreationOptionsExtensions
{
    public static void UseAutofac(this AbpApplicationCreationOptions options)
    {
        options.Services.AddAutofacServiceProviderFactory();
    }

    public static AbpAutofacServiceProviderFactory AddAutofacServiceProviderFactory(this IServiceCollection services)
    {
        return services.AddAutofacServiceProviderFactory(new ContainerBuilder());
    }

    public static AbpAutofacServiceProviderFactory AddAutofacServiceProviderFactory(this IServiceCollection services, ContainerBuilder containerBuilder)
    {
        var factory = new AbpAutofacServiceProviderFactory(containerBuilder);

        services.AddObjectAccessor(containerBuilder);
        services.AddSingleton((IServiceProviderFactory<ContainerBuilder>)factory);
        services.AddAbpAutofacUnnamedOptionsManager();

        return factory;
    }

    internal static void AddAbpAutofacUnnamedOptionsManager(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton(typeof(IOptions<>), typeof(AbpAutofacUnnamedOptionsManager<>)));
    }
}