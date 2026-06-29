using Lumen.Domain.Audit;
using Lumen.Modularity;
using Lumen.Modules.Audit.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class UserLoggedInAuditHandler : INotificationHandler<UserLoggedIn>
{
    private readonly IEventBus _eventBus;

    public UserLoggedInAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(UserLoggedIn notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new UserLoggedInEvent(notification.UserId, notification.Username),
            cancellationToken);
}
