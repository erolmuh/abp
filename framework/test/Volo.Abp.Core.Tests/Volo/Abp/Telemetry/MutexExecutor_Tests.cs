using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Telemetry.Helpers;
using Xunit;

namespace Volo.Abp.Telemetry;

public class MutexExecutor_Tests
{
    [Fact]
    public async Task Should_Execute_Action_Successfully()
    {
        // Arrange
        var expectedResult = "test result";

        // Act
        var result = await MutexExecutor.ExecuteAsync("test_mutex",
            () => Task.FromResult(expectedResult));

        // Assert
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task Should_Handle_Failed_Action()
    {
        // Act
        var result = await MutexExecutor.ExecuteAsync<string>("test_mutex_failed",
            () => throw new IOException());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Execute_Multiple_Actions_Sequentially()
    {
        // Arrange
        var mutexName = "sequential_test_mutex";
        var counter = 0;

        // Act
        var task1 = MutexExecutor.ExecuteAsync(mutexName, async () =>
        {
            await Task.Delay(100);
            return ++counter;
        });

        var task2 = MutexExecutor.ExecuteAsync(mutexName, async () =>
        {
            await Task.Delay(50);
            return ++counter;
        });

        await Task.WhenAll(task1, task2);

        // Assert
        counter.ShouldBe(2);
    }
}