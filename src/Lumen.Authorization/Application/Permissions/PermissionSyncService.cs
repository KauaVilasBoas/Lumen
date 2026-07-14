using Lumen.Authorization.Domain;
using Microsoft.Extensions.Logging;

namespace Lumen.Authorization.Application.Permissions;

internal sealed class PermissionSyncService : IPermissionSyncService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IGroupPermissionRepository _groupPermissionRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(
        IPermissionRepository permissionRepository,
        IGroupPermissionRepository groupPermissionRepository,
        IProfileRepository profileRepository,
        ILogger<PermissionSyncService> logger)
    {
        _permissionRepository = permissionRepository;
        _groupPermissionRepository = groupPermissionRepository;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task SyncDiscoveredAsync(IReadOnlyList<DiscoveredPermissionEntry> discovered, CancellationToken ct = default)
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
                existing.UpdateLocationAndGroup(item.Controller, item.Action, groupId);

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
                var permission = Permission.Create(item.Controller, item.Action, groupId);
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

    public async Task ReconcileAdministratorAsync(CancellationToken ct = default)
    {
        var administratorProfile = await _profileRepository
            .FindByIdAsync(SystemProfiles.AdministratorId, ct);

        if (administratorProfile is null)
        {
            _logger.LogError(
                "Administrator profile (Id={ProfileId}) not found. " +
                "Ensure the SeedDefaultProfiles migration has been applied.",
                SystemProfiles.AdministratorId);
            return;
        }

        var allPermissions = await _permissionRepository.ListAllAsync(ct);

        var existingAssignments = await _profileRepository
            .GetPermissionProfilesByProfileIdAsync(SystemProfiles.AdministratorId, ct);

        var assignedPermissionIds = new HashSet<Guid>(existingAssignments.Select(pp => pp.PermissionId));

        var permissionsToGrant = allPermissions
            .Where(p => !p.IsDeleted && !assignedPermissionIds.Contains(p.Id))
            .ToList();

        if (permissionsToGrant.Count == 0)
        {
            _logger.LogInformation(
                "Administrator profile already holds all {Count} permission(s). No reconciliation needed.",
                allPermissions.Count);
            return;
        }

        var newAssignments = permissionsToGrant
            .Select(p => PermissionProfile.Create(p.Id, SystemProfiles.AdministratorId))
            .ToList();

        await _profileRepository.InsertPermissionProfilesAsync(newAssignments, ct);

        _logger.LogInformation(
            "Administrator permission reconciliation granted {Granted} new permission(s). " +
            "Total permissions held: {Total}.",
            newAssignments.Count,
            assignedPermissionIds.Count + newAssignments.Count);
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
