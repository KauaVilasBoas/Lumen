using AegisIdentity.Domain.Authorization;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Authorization;

public sealed class UserPermissionsChangedHandler : INotificationHandler<UserPermissionsChanged>
{
    private readonly IUserPermissionCache _cache;
    private readonly ILogger<UserPermissionsChangedHandler> _logger;

    public UserPermissionsChangedHandler(
        IUserPermissionCache cache,
        ILogger<UserPermissionsChangedHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(UserPermissionsChanged notification, CancellationToken cancellationToken)
    {
        await _cache.InvalidateAsync(notification.UserId, cancellationToken);

        _logger.LogInformation(
            "Permission cache invalidated for user {UserId} due to profile/permission change.",
            notification.UserId);
    }
}
