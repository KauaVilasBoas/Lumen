using Lumen.Domain.Audit;
using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class UserProfileRemovedAuditHandler : INotificationHandler<UserProfileRemoved>
{
    private readonly IEventBus _eventBus;

    public UserProfileRemovedAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(UserProfileRemoved notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new UserProfileRemovedEvent(
                notification.UserId,
                notification.Username,
                notification.ProfileId,
                notification.ProfileName),
            cancellationToken);
}
