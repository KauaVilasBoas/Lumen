using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lumen.Api.Authorization;

public sealed class AdministratorPermissionReconciliationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdministratorPermissionReconciliationHostedService> _logger;

    public AdministratorPermissionReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<AdministratorPermissionReconciliationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Administrator permission reconciliation...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var reconciliationService = scope.ServiceProvider
            .GetRequiredService<AdministratorPermissionReconciliationService>();

        await reconciliationService.ReconcileAsync(cancellationToken);

        _logger.LogInformation("Administrator permission reconciliation completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
