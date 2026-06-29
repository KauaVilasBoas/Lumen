using Lumen.Domain.Authorization;
using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class UserPermissionsChangedAuditHandler : INotificationHandler<UserPermissionsChanged>
{
    private readonly IEventBus _eventBus;

    public UserPermissionsChangedAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(UserPermissionsChanged notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new UserPermissionsChangedEvent(notification.UserId),
            cancellationToken);
}
