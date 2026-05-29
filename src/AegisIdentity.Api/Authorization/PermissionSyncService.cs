using AegisIdentity.Domain.Authorization;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Api.Authorization;

public sealed class PermissionSyncService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(
        IPermissionRepository permissionRepository,
        IGroupPermissionRepository groupPermissionRepository,
        ILogger<PermissionSyncService> logger)
    {
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
        _logger = logger;
    }

    public async Task SyncAsync(
        IReadOnlyList<DiscoveredPermission> discovered,
        CancellationToken ct = default)
    {
        var existingPermissions = (await _permissionRepository.ListAllAsync(ct))
            .ToDictionary(p => p.Code, StringComparer.Ordinal);

        var discoveredCodes = new HashSet<string>(
            discovered.Select(d => d.Code),
            StringComparer.Ordinal);

        foreach (var item in discovered)
        {
            var groupId = await UpsertGroupAsync(item.GroupName, ct);

            if (existingPermissions.TryGetValue(item.Code, out var existing))
            {
                existing.Update(item.Controller, item.Action, item.DisplayName, groupId);

                if (existing.IsOrphan)
                {
                    existing.ClearOrphan();
                    _logger.LogInformation(
                        "Permission '{Code}' was previously orphaned and has been rediscovered.",
                        item.Code);
                }

                await _permissionRepository.UpdateAsync(existing, ct);
            }
            else
            {
                var permission = Permission.Create(item.Controller, item.Action, item.DisplayName, groupId);
                await _permissionRepository.InsertAsync(permission, ct);

                _logger.LogInformation("Registered new permission '{Code}'.", item.Code);
            }
        }

        var orphaned = existingPermissions.Values
            .Where(p => !discoveredCodes.Contains(p.Code) && !p.IsOrphan)
            .ToList();

        foreach (var orphan in orphaned)
        {
            orphan.MarkAsOrphan();

            _logger.LogWarning(
                "Permission '{Code}' has no corresponding action and has been marked as orphan. " +
                "It will NOT be deleted as it may be referenced by profiles.",
                orphan.Code);
        }

        if (orphaned.Count > 0)
            await _permissionRepository.SaveAllAsync(orphaned, ct);
    }

    private async Task<Guid?> UpsertGroupAsync(string groupName, CancellationToken ct)
    {
        var existing = await _groupPermissionRepository.FindByNameAsync(groupName, ct);

        if (existing is not null)
            return existing.Id;

        var group = GroupPermission.Create(groupName, groupName);
        await _groupPermissionRepository.InsertAsync(group, ct);

        return group.Id;
    }
}
