using Lumen.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lumen.Identity.Migrations;

/// <summary>
/// Hosted service that applies pending Lumen.Identity EF Core migrations on startup.
/// Registered automatically by <see cref="LumenIdentityMigrationsServiceCollectionExtensions.AddLumenIdentityMigrations"/>.
/// </summary>
public sealed class LumenIdentityMigrationsHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LumenIdentityMigrationsHostedService> _logger;

    public LumenIdentityMigrationsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<LumenIdentityMigrationsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying Lumen.Identity EF Core migrations...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count == 0)
        {
            _logger.LogInformation("No pending Lumen.Identity migrations.");
            return;
        }

        _logger.LogInformation(
            "Applying {Count} pending Lumen.Identity migration(s): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Lumen.Identity migrations applied successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
