using Lumen.Domain.Audit;
using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class UserLockedOutAuditHandler : INotificationHandler<UserLockedOut>
{
    private readonly IEventBus _eventBus;

    public UserLockedOutAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(UserLockedOut notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new UserLockedOutEvent(notification.UserId, notification.Username),
            cancellationToken);
}
