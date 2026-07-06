using Lumen.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lumen.Identity.Migrations.PostgreSQL;

internal sealed class LumenIdentityPostgresMigrationsHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LumenIdentityPostgresMigrationsHostedService> _logger;

    public LumenIdentityPostgresMigrationsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<LumenIdentityPostgresMigrationsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying Lumen.Identity EF Core migrations (PostgreSQL)...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count == 0)
        {
            _logger.LogInformation("No pending Lumen.Identity migrations (PostgreSQL).");
            return;
        }

        _logger.LogInformation(
            "Applying {Count} pending Lumen.Identity migration(s) (PostgreSQL): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Lumen.Identity migrations applied successfully (PostgreSQL).");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
