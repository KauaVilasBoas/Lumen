using Lumen.Authorization.Application.Permissions;
using Microsoft.Extensions.Logging;

namespace Lumen.Api.Authorization;

internal sealed class AdministratorPermissionReconciliationService
{
    private readonly IPermissionSyncService _authorizationPermissionSyncService;
    private readonly ILogger<AdministratorPermissionReconciliationService> _logger;

    public AdministratorPermissionReconciliationService(
        IPermissionSyncService authorizationPermissionSyncService,
        ILogger<AdministratorPermissionReconciliationService> logger)
    {
        _authorizationPermissionSyncService = authorizationPermissionSyncService;
        _logger = logger;
    }

    public Task ReconcileAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running Administrator profile permission reconciliation via Authorization library.");
        return _authorizationPermissionSyncService.ReconcileAdministratorAsync(ct);
    }
}
