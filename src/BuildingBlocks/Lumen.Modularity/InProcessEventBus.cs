using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lumen.Modularity;

internal sealed class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessEventBus> _logger;

    public InProcessEventBus(IServiceProvider serviceProvider, ILogger<InProcessEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventType = typeof(TEvent);

        _logger.LogDebug("Publishing integration event {EventType} ({EventId})", eventType.Name, integrationEvent.EventId);

        using var scope = _serviceProvider.CreateScope();

        var handlerType = typeof(IIntegrationEventHandler<TEvent>);
        var handlers = scope.ServiceProvider.GetServices(handlerType).Cast<IIntegrationEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }
}
