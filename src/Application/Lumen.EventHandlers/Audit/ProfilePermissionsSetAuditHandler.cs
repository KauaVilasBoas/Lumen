using Lumen.Domain.Audit;
using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using MediatR;

namespace Lumen.EventHandlers.Audit;

public sealed class ProfilePermissionsSetAuditHandler : INotificationHandler<ProfilePermissionsSet>
{
    private readonly IEventBus _eventBus;

    public ProfilePermissionsSetAuditHandler(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task Handle(ProfilePermissionsSet notification, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            new ProfilePermissionsSetEvent(
                notification.ProfileId,
                notification.ProfileName,
                notification.ActorUsername),
            cancellationToken);
}
