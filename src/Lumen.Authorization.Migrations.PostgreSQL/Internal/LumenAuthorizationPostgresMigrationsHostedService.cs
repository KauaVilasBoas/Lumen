using Lumen.Authorization;
using Lumen.Authorization.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.Migrations.PostgreSQL.Internal;

internal sealed class LumenAuthorizationPostgresMigrationsHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LumenAuthorizationPostgresMigrationsHostedService> _logger;
    private readonly LumenAuthorizationOptions _options;

    public LumenAuthorizationPostgresMigrationsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<LumenAuthorizationPostgresMigrationsHostedService> logger,
        IOptions<LumenAuthorizationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _options      = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.ApplyMigrationsOnStartup)
        {
            _logger.LogInformation("Lumen Authorization auto-migration skipped (ApplyMigrationsOnStartup = false).");
            return;
        }

        _logger.LogInformation("Applying Lumen Authorization EF Core migrations (PostgreSQL)...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LumenAuthorizationDbContext>();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pendingMigrations.Count == 0)
        {
            _logger.LogInformation("No pending Lumen Authorization migrations (PostgreSQL).");
            return;
        }

        _logger.LogInformation(
            "Applying {Count} pending Lumen Authorization migration(s) (PostgreSQL): {Migrations}",
            pendingMigrations.Count,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Lumen Authorization migrations applied successfully (PostgreSQL).");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
