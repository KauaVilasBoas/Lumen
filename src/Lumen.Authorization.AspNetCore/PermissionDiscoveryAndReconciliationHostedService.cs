using Lumen.Authorization.Application.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lumen.Authorization.AspNetCore;

public sealed class PermissionDiscoveryAndReconciliationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PermissionDiscoveryScanner _scanner;
    private readonly ILogger<PermissionDiscoveryAndReconciliationHostedService> _logger;

    public PermissionDiscoveryAndReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        PermissionDiscoveryScanner scanner,
        ILogger<PermissionDiscoveryAndReconciliationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _scanner = scanner;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting permission discovery...");

        var discovered = _scanner.Scan();

        _logger.LogInformation("Discovered {Count} action(s) decorated with [RequirePermission].", discovered.Count);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IPermissionSyncService>();

        await syncService.SyncDiscoveredAsync(discovered, cancellationToken);

        _logger.LogInformation("Permission discovery and sync completed. Running Administrator reconciliation...");

        await syncService.ReconcileAdministratorAsync(cancellationToken);

        _logger.LogInformation("Permission discovery and reconciliation completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
