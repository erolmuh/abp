using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Autofac;
using Volo.Abp.Json.SystemTextJson;
using Xunit;

namespace Volo.Abp.Json;

public class AbpJsonSystemTextJson_DeadLock_Tests : AbpJsonSystemTextJsonTestBase
{
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);

        services.AddSingleton(new ThreadSynchronizationEvents());
        services.AddSingleton<TestSingletonService, TestSingletonService>();
    }

    /// <summary>
    /// Simulates a deadlock scenario that may occur when a singleton service (TestSingletonService) and
    /// direct IOptions resolution concurrently access interdependent options configurations.
    ///
    /// The test coordinates thread execution using synchronization events to create a race condition
    /// between option value resolution paths. Verifies the framework's ability to handle concurrent
    /// option dependencies without deadlocks.
    ///
    /// For implementation details, see <see cref="AbpAutofacUnnamedOptionsManager{TOptions}"/>.
    /// </summary>
    [Fact]
    public async Task Should_Not_Deadlock_On_Concurrent_Dependency_Resolution()
    {
        // Arrange
        var threadSynchronizationEvents = GetRequiredService<ThreadSynchronizationEvents>();

        var thread1 = new Thread(() =>
        {
            // Resolves TestSingletonService which has a dependency chain:
            // TestSingletonService -> IOptions<AbpSystemTextJsonSerializerOptions> ->
            // IOptions<AbpSystemTextJsonSerializerModifiersOptions>

            using var scope = TestServiceScope.ServiceProvider.CreateScope();
            var _ = scope.ServiceProvider.GetRequiredService<TestSingletonService>();
        });

        var thread2 = new Thread(() =>
        {
            // Directly resolves IOptions<AbpSystemTextJsonSerializerOptions>
            using var scope = TestServiceScope.ServiceProvider.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<AbpSystemTextJsonSerializerOptions>>();

            // Signal main thread before accessing Value to create synchronization point
            threadSynchronizationEvents.Thread2Event.Set();

            var _ = options.Value; // Potential deadlock point
        });

        // Act
        thread1.Start();
        threadSynchronizationEvents.Thread1ObtainedIOptionsEvent.Wait(); // Wait for Thread1 to reach value access point

        thread2.Start();
        threadSynchronizationEvents.Thread2Event.Wait(); // Wait for Thread2 to reach value access point

        Thread.Sleep(TimeSpan.FromSeconds(1)); // Allow potential deadlock state to manifest

        // Release Thread1 to proceed with value access
        threadSynchronizationEvents.Thread1ContinueSignalEvent.Set();

        // Assert
        thread1.Join(TimeSpan.FromSeconds(45)).ShouldBeTrue("Thread 1 deadlocked during options resolution");
        thread2.Join(TimeSpan.FromSeconds(45)).ShouldBeTrue("Thread 2 deadlocked during options resolution");
    }

    private class ThreadSynchronizationEvents
    {
        // Signals when Thread1 has resolved IOptions but before accessing Value
        public ManualResetEventSlim Thread1ObtainedIOptionsEvent { get; init; } = new();

        // Allows main thread to control when Thread1 proceeds to access Value
        public ManualResetEventSlim Thread1ContinueSignalEvent { get; init; } = new();

        // Signals when Thread2 has resolved IOptions and is about to access Value
        public ManualResetEventSlim Thread2Event { get; init; } = new();
    }

    private class TestSingletonService
    {
        private readonly AbpSystemTextJsonSerializerOptions _jsonSerializerOptions;

        public TestSingletonService(
            IOptions<AbpSystemTextJsonSerializerOptions> jsonSerializerOptions,
            ThreadSynchronizationEvents threadSynchronizationEvents)
        {
            // Notify main thread that IOptions resolution is complete
            threadSynchronizationEvents.Thread1ObtainedIOptionsEvent.Set();

            // Wait for controlled release to access Value
            threadSynchronizationEvents.Thread1ContinueSignalEvent.Wait();

            _jsonSerializerOptions = jsonSerializerOptions.Value; // Contended access point
        }
    }
}