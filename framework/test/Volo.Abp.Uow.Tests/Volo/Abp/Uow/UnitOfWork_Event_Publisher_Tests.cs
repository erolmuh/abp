using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using NSubstitute;
using Volo.Abp.Testing;

namespace Volo.Abp.Uow;

public class UnitOfWork_Event_Publisher_Tests : AbpIntegratedTest<AbpUnitOfWorkModule>
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IUnitOfWorkEventPublisher _mockedUnitOfWorkEventPublisher;

    public UnitOfWork_Event_Publisher_Tests()
    {
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _mockedUnitOfWorkEventPublisher = GetRequiredService<IUnitOfWorkEventPublisher>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(Substitute.For<IUnitOfWorkEventPublisher>());
        base.AfterAddApplication(services);
    }

    [Fact]
    public async Task Should_Publish_Nested_Generated_Distributed_Event_On_Complete_Async()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();

        var localEventA = new TestEventData();
        var distributedEventB = new TestEventData();
        var nestedDistributedEventC = new TestEventData();

        _mockedUnitOfWorkEventPublisher
            .PublishLocalEventsAsync(Arg.Any<IEnumerable<UnitOfWorkEventRecord>>())
            .Returns(_ =>
            {
                /* This code simulates a ILocalEventHandler<> that publishes a new distributed event while
                /* the code is running inside a UnitOfWork.CompleteAsync() call
                */

                uow.AddOrReplaceDistributedEvent(new UnitOfWorkEventRecord(nestedDistributedEventC.GetType(),
                    nestedDistributedEventC, 2));

                return Task.CompletedTask;
            });

        uow.AddOrReplaceLocalEvent(new UnitOfWorkEventRecord(localEventA.GetType(), localEventA, 0));
        uow.AddOrReplaceDistributedEvent(new UnitOfWorkEventRecord(distributedEventB.GetType(), distributedEventB, 1));

        // Act
        await uow.CompleteAsync();

        // Assert
        await _mockedUnitOfWorkEventPublisher.Received(1).PublishLocalEventsAsync(
            Arg.Is<IEnumerable<UnitOfWorkEventRecord>>(events => events.Any(x => x.EventData == localEventA)));

        await _mockedUnitOfWorkEventPublisher.Received(1).PublishDistributedEventsAsync(
            Arg.Is<IEnumerable<UnitOfWorkEventRecord>>(events => events.Any(x => x.EventData == distributedEventB)));

        await _mockedUnitOfWorkEventPublisher.Received(1).PublishDistributedEventsAsync(
            Arg.Is<IEnumerable<UnitOfWorkEventRecord>>(events => events.Any(x => x.EventData == nestedDistributedEventC)));
    }

    private class TestEventData
    {
    }
}