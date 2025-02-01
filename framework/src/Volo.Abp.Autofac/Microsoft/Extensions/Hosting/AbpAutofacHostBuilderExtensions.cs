using Autofac;
using Volo.Abp.Autofac;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Microsoft.Extensions.Hosting;

public static class AbpAutofacHostBuilderExtensions
{
    public static IHostBuilder UseAutofac(this IHostBuilder hostBuilder)
    {
        var containerBuilder = new ContainerBuilder();

        return hostBuilder.ConfigureServices((_, services) =>
            {
                services.AddObjectAccessor(containerBuilder);
                services.AddAbpAutofacUnnamedOptionsManager();
            })
            .UseServiceProviderFactory(new AbpAutofacServiceProviderFactory(containerBuilder));
    }
}