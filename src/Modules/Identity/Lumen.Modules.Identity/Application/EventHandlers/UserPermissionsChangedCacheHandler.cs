using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Authorization;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.EventHandlers;

internal sealed class UserPermissionsChangedCacheHandler : IIntegrationEventHandler<UserPermissionsChangedEvent>
{
    private readonly IUserPermissionCache _cache;
    private readonly ILogger<UserPermissionsChangedCacheHandler> _logger;

    public UserPermissionsChangedCacheHandler(
        IUserPermissionCache cache,
        ILogger<UserPermissionsChangedCacheHandler> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task HandleAsync(UserPermissionsChangedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await _cache.InvalidateAsync(integrationEvent.UserId, cancellationToken);

        _logger.LogInformation(
            "Permission cache invalidated for user {UserId} due to profile/permission change.",
            integrationEvent.UserId);
    }
}
