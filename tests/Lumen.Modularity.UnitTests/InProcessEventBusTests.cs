using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Lumen.Modularity;

namespace Lumen.Modularity.UnitTests;

public sealed class InProcessEventBusTests
{
    [Fact]
    public async Task PublishAsync_WhenHandlerIsRegistered_InvokesHandler()
    {
        var handler = new RecordingEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler<OrderPlacedEvent>>(handler);
        var provider = services.BuildServiceProvider();

        var bus = new InProcessEventBus(provider, NullLogger<InProcessEventBus>.Instance);
        var integrationEvent = new OrderPlacedEvent("order-1");

        await bus.PublishAsync(integrationEvent);

        handler.ReceivedEvents.Should().ContainSingle()
            .Which.OrderId.Should().Be("order-1");
    }

    [Fact]
    public async Task PublishAsync_WhenMultipleHandlersAreRegistered_InvokesAllHandlers()
    {
        var firstHandler = new RecordingEventHandler();
        var secondHandler = new RecordingEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler<OrderPlacedEvent>>(firstHandler);
        services.AddSingleton<IIntegrationEventHandler<OrderPlacedEvent>>(secondHandler);
        var provider = services.BuildServiceProvider();

        var bus = new InProcessEventBus(provider, NullLogger<InProcessEventBus>.Instance);
        var integrationEvent = new OrderPlacedEvent("order-2");

        await bus.PublishAsync(integrationEvent);

        firstHandler.ReceivedEvents.Should().HaveCount(1);
        secondHandler.ReceivedEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task PublishAsync_WhenNoHandlerIsRegistered_CompletesWithoutError()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var bus = new InProcessEventBus(provider, NullLogger<InProcessEventBus>.Instance);
        var integrationEvent = new OrderPlacedEvent("order-3");

        var act = () => bus.PublishAsync(integrationEvent);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenEventHasCorrectMetadata_HandlerReceivesConsistentEvent()
    {
        var handler = new RecordingEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler<OrderPlacedEvent>>(handler);
        var provider = services.BuildServiceProvider();

        var bus = new InProcessEventBus(provider, NullLogger<InProcessEventBus>.Instance);
        var integrationEvent = new OrderPlacedEvent("order-4");

        await bus.PublishAsync(integrationEvent);

        var received = handler.ReceivedEvents.Single();
        received.EventId.Should().Be(integrationEvent.EventId);
        received.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerForDifferentEventType_DoesNotInvokeHandler()
    {
        var handler = new RecordingEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler<OrderPlacedEvent>>(handler);
        var provider = services.BuildServiceProvider();

        var bus = new InProcessEventBus(provider, NullLogger<InProcessEventBus>.Instance);
        var unrelatedEvent = new UnrelatedEvent();

        await bus.PublishAsync(unrelatedEvent);

        handler.ReceivedEvents.Should().BeEmpty();
    }
}

public sealed record OrderPlacedEvent(string OrderId) : IntegrationEvent;

public sealed record UnrelatedEvent : IntegrationEvent;

public sealed class RecordingEventHandler : IIntegrationEventHandler<OrderPlacedEvent>
{
    public List<OrderPlacedEvent> ReceivedEvents { get; } = [];

    public Task HandleAsync(OrderPlacedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ReceivedEvents.Add(integrationEvent);
        return Task.CompletedTask;
    }
}
