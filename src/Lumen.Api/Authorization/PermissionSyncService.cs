using Lumen.Modules.Identity.Application.Permissions;
using Microsoft.Extensions.Logging;

namespace Lumen.Api.Authorization;

internal sealed class PermissionSyncService
{
    private readonly IPermissionSyncService _identityPermissionSyncService;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(
        IPermissionSyncService identityPermissionSyncService,
        ILogger<PermissionSyncService> logger)
    {
        _identityPermissionSyncService = identityPermissionSyncService;
        _logger = logger;
    }

    public Task SyncAsync(IReadOnlyList<DiscoveredPermission> discovered, CancellationToken ct = default)
    {
        _logger.LogInformation("Syncing {Count} discovered permission(s) via Identity module.", discovered.Count);

        var entries = discovered
            .Select(d => new DiscoveredPermissionEntry(
                Controller: d.Controller,
                Action: d.Action,
                DisplayName: d.DisplayName,
                Code: d.Code,
                GroupName: d.GroupName))
            .ToList();

        return _identityPermissionSyncService.SyncDiscoveredAsync(entries, ct);
    }
}
