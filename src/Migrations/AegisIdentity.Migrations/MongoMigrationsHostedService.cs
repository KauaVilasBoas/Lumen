using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Migrations;

public sealed class MongoMigrationsHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MongoMigrationsHostedService> _logger;

    public MongoMigrationsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MongoMigrationsHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Mongo migrations on startup...");

        // Hosted services are Singletons; the runner depends on Scoped
        // IMongoDatabase, so we open a dedicated scope here.
        using var scope = _scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<MongoMigrationRunner>();
        var applied = await runner.ApplyPendingAsync(cancellationToken);

        _logger.LogInformation("Mongo migrations completed: {Count} applied.", applied);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
