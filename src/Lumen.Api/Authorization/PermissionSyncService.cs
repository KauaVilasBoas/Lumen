using Lumen.Authorization.Application.Permissions;
using Microsoft.Extensions.Logging;

namespace Lumen.Api.Authorization;

internal sealed class PermissionSyncService
{
    private readonly IPermissionSyncService _authorizationPermissionSyncService;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(
        IPermissionSyncService authorizationPermissionSyncService,
        ILogger<PermissionSyncService> logger)
    {
        _authorizationPermissionSyncService = authorizationPermissionSyncService;
        _logger = logger;
    }

    public Task SyncAsync(IReadOnlyList<DiscoveredPermission> discovered, CancellationToken ct = default)
    {
        _logger.LogInformation("Syncing {Count} discovered permission(s) via Authorization library.", discovered.Count);

        var entries = discovered
            .Select(d => new DiscoveredPermissionEntry(
                Controller: d.Controller,
                Action: d.Action,
                DisplayName: d.DisplayName,
                Code: d.Code,
                GroupName: d.GroupName))
            .ToList();

        return _authorizationPermissionSyncService.SyncDiscoveredAsync(entries, ct);
    }
}
