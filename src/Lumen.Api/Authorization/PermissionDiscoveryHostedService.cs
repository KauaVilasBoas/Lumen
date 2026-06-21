using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lumen.Api.Authorization;

public sealed class PermissionDiscoveryHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PermissionDiscoveryScanner _scanner;
    private readonly ILogger<PermissionDiscoveryHostedService> _logger;

    public PermissionDiscoveryHostedService(
        IServiceScopeFactory scopeFactory,
        PermissionDiscoveryScanner scanner,
        ILogger<PermissionDiscoveryHostedService> logger)
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
        var syncService = scope.ServiceProvider.GetRequiredService<PermissionSyncService>();

        await syncService.SyncAsync(discovered, cancellationToken);

        _logger.LogInformation("Permission discovery completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
