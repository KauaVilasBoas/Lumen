using Lumen.Domain.Audit;
using Lumen.Modularity;
using Lumen.Modules.Audit.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class UserProfileAssignedAuditHandler : INotificationHandler<UserProfileAssigned>
{
    private readonly IEventBus _eventBus;

    public UserProfileAssignedAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(UserProfileAssigned notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new UserProfileAssignedEvent(
                notification.UserId,
                notification.Username,
                notification.ProfileId,
                notification.ProfileName),
            cancellationToken);
}
