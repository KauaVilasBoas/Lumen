using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Modularity;
using Microsoft.Extensions.Logging;

namespace Lumen.Authorization.Application.EventHandlers;

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
        await _cache.InvalidateAsync(integrationEvent.UserId, integrationEvent.ScopeId, cancellationToken);

        _logger.LogInformation(
            "Permission cache invalidated for user {UserId} scope {ScopeId} due to profile/permission change.",
            integrationEvent.UserId,
            integrationEvent.ScopeId);
    }
}
