using Lumen.Domain.Audit;
using Lumen.Modularity;
using Lumen.Modules.Audit.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class CleanupJobExecutedAuditHandler : INotificationHandler<CleanupJobExecuted>
{
    private readonly IEventBus _eventBus;

    public CleanupJobExecutedAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(CleanupJobExecuted notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new CleanupJobExecutedEvent(notification.JobName, notification.DeletedCount),
            cancellationToken);
}
