using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Testing;
using Xunit;

namespace Volo.Abp.Autofac;

public class Autofac_Dead_Lock_Tests : AbpIntegratedTest<AutofacTestModule>
{
    private readonly ManualResetEventSlim _optionsAReadyToContinueEvent = new ();

    private readonly ManualResetEventSlim _optionsAContinueEvent = new ();

    private readonly ManualResetEventSlim _optionsBEvent = new ();

    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);

        services.AddSingleton<SingletonTestService, SingletonTestService>();

        services.AddOptions<OptionsA>()
            .Configure<IServiceProvider>((optionsA, rootServiceProvider) =>
            {
                var optionsB = rootServiceProvider.GetRequiredService<IOptions<OptionsB>>();

                _optionsAReadyToContinueEvent.Set();
                _optionsAContinueEvent.Wait();

                optionsA.OptionsB = optionsB.Value;
            });

        services.AddOptions<OptionsB>()
            .Configure<IServiceProvider>((optionsB, rootServiceProvider) =>
            {
                _optionsBEvent.Set();
                optionsB.OptionsC = rootServiceProvider.GetRequiredService<IOptions<OptionsC>>().Value;
            });

        services.AddOptions<OptionsC>();
    }

    /// <summary>
    /// This test simulates a deadlock scenario that can occur when a specific SingletonService depends on IOptions A,
    /// which in turn depends on IOptions B, and B depends on IOptions C.
    /// This mirrors the dependency chain of:
    /// AbpSystemTextJsonSerializerOptions -> AbpSystemTextJsonSerializerModifiersOptions -> AbpJsonOptions.
    ///
    /// The test coordinates two threads using events to ensure a reproducible deadlock situation.
    ///
    /// For more details, see the commentary in <see cref="AbpAutofacUnnamedOptionsManager{TOptions}"/>.
    /// </summary>
    [Fact]
    public async Task Should_Not_Deadlock_On_Concurrent_Dependency_Resolution()
    {
        // Arrange
        var thread1 = new Thread(() =>
        {
            using var scope = TestServiceScope.ServiceProvider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<SingletonTestService>();
        });

        var thread2 = new Thread(() =>
        {
            using var scope = TestServiceScope.ServiceProvider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<IOptions<OptionsB>>().Value;
        });

        // Act
        thread1.Start();
        _optionsAReadyToContinueEvent.Wait();

        thread2.Start();
        _optionsBEvent.Wait();
        _optionsAContinueEvent.Set();

        // Assert
        thread1.Join(TimeSpan.FromSeconds(60)).ShouldBeTrue("Thread 1 is deadlocked");
        thread2.Join(TimeSpan.FromSeconds(60)).ShouldBeTrue("Thread 2 is deadlocked");
    }

    private class SingletonTestService
    {
        private readonly OptionsA _optionsA;

        public SingletonTestService(IOptions<OptionsA> optionsA)
        {
            _optionsA = optionsA.Value;
        }
    }

    private class OptionsA
    {
        public OptionsB OptionsB { get; set; }
    }

    private class OptionsB
    {
        public OptionsC OptionsC { get; set; }
    }

    private class OptionsC
    {

    }
}