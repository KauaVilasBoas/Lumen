using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using MediatR;

namespace Lumen.EventHandlers.Authorization;

public sealed class ProfilePermissionsSetCacheInvalidationHandler : INotificationHandler<ProfilePermissionsSet>
{
    private readonly IPublisher _publisher;

    public ProfilePermissionsSetCacheInvalidationHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(ProfilePermissionsSet notification, CancellationToken cancellationToken)
    {
        foreach (var userId in notification.AffectedUserIds)
            await _publisher.Publish(new UserPermissionsChanged(userId), cancellationToken);
    }
}
