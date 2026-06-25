using Lumen.Domain.Authorization;
using MediatR;

namespace Lumen.EventHandlers.Authorization;

public sealed class ProfileDeletedCacheInvalidationHandler : INotificationHandler<ProfileDeleted>
{
    private readonly IPublisher _publisher;

    public ProfileDeletedCacheInvalidationHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(ProfileDeleted notification, CancellationToken cancellationToken)
    {
        foreach (var userId in notification.AffectedUserIds)
            await _publisher.Publish(new UserPermissionsChanged(userId), cancellationToken);
    }
}
