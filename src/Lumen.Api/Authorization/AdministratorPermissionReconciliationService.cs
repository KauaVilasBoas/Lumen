using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Api.Authorization;

public sealed class AdministratorPermissionReconciliationService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<AdministratorPermissionReconciliationService> _logger;

    public AdministratorPermissionReconciliationService(
        IPermissionRepository permissionRepository,
        IProfileRepository profileRepository,
        ILogger<AdministratorPermissionReconciliationService> logger)
    {
        _permissionRepository = permissionRepository;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task ReconcileAsync(CancellationToken ct = default)
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
}
