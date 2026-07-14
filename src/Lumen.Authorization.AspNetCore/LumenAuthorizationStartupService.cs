using Lumen.Authorization.Application.Permissions;
using Lumen.Authorization.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.AspNetCore;

public sealed class LumenAuthorizationStartupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PermissionDiscoveryScanner _scanner;
    private readonly LumenAuthorizationOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LumenAuthorizationStartupService> _logger;

    public LumenAuthorizationStartupService(
        IServiceScopeFactory scopeFactory,
        PermissionDiscoveryScanner scanner,
        IOptions<LumenAuthorizationOptions> options,
        ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _scanner = scanner;
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<LumenAuthorizationStartupService>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ApplyMigrationsAsync(cancellationToken);
        await RunCatalogOperationAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ApplyMigrationsAsync(CancellationToken cancellationToken)
    {
        if (!_options.ApplyMigrationsOnStartup)
        {
            _logger.LogInformation("Lumen Authorization auto-migration skipped (ApplyMigrationsOnStartup = false).");
            return;
        }

        _logger.LogInformation("Applying Lumen Authorization EF Core migrations...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LumenAuthorizationDbContext>();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count == 0)
        {
            _logger.LogInformation("No pending Lumen Authorization migrations.");
            return;
        }

        _logger.LogInformation(
            "Applying {Count} pending Lumen Authorization migration(s): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Lumen Authorization migrations applied successfully.");
    }

    private Task RunCatalogOperationAsync(CancellationToken cancellationToken) =>
        _options.CatalogMode switch
        {
            PermissionCatalogMode.Off => LogAndSkipCatalogAsync(),
            PermissionCatalogMode.Validate => RunValidationAsync(cancellationToken),
            PermissionCatalogMode.Sync => RunSyncAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(_options.CatalogMode),
                _options.CatalogMode,
                null)
        };

    private Task LogAndSkipCatalogAsync()
    {
        _logger.LogInformation("Lumen Authorization catalog operation skipped (CatalogMode = Off).");
        return Task.CompletedTask;
    }

    private async Task RunValidationAsync(CancellationToken cancellationToken)
    {
        var validationLogger = _loggerFactory.CreateLogger<PermissionCatalogValidationService>();
        var validationService = new PermissionCatalogValidationService(
            _scopeFactory,
            _scanner,
            _options.FailFastOnMissingPermission,
            validationLogger);

        await validationService.ValidateAsync(cancellationToken);
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        var discovered = _scanner.Scan();

        _logger.LogInformation(
            "Lumen Authorization catalog sync: discovered {Count} action(s) with [RequirePermission].",
            discovered.Count);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IPermissionSyncService>();

        await syncService.SyncDiscoveredAsync(discovered, cancellationToken);

        _logger.LogInformation("Lumen Authorization catalog sync completed.");

        if (_options.AutoGrantAllToAdministrator)
        {
            _logger.LogInformation("Running Administrator permission reconciliation...");
            await syncService.ReconcileAdministratorAsync(cancellationToken);
            _logger.LogInformation("Administrator permission reconciliation completed.");
        }
    }
}
