using Lumen.Modules.Identity.Application.Permissions;
using Microsoft.Extensions.Logging;

namespace Lumen.Api.Authorization;

internal sealed class AdministratorPermissionReconciliationService
{
    private readonly IPermissionSyncService _identityPermissionSyncService;
    private readonly ILogger<AdministratorPermissionReconciliationService> _logger;

    public AdministratorPermissionReconciliationService(
        IPermissionSyncService identityPermissionSyncService,
        ILogger<AdministratorPermissionReconciliationService> logger)
    {
        _identityPermissionSyncService = identityPermissionSyncService;
        _logger = logger;
    }

    public Task ReconcileAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running Administrator profile permission reconciliation via Identity module.");
        return _identityPermissionSyncService.ReconcileAdministratorAsync(ct);
    }
}
