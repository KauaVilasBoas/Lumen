using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AegisIdentity.Migrations;

public sealed class MongoMigrationRunner
{
    private readonly IMongoDatabase _database;
    private readonly MongoMigrationHistoryRepository _history;
    private readonly IReadOnlyList<IMongoMigration> _migrations;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MongoMigrationRunner> _logger;

    public MongoMigrationRunner(
        IMongoDatabase database,
        MongoMigrationHistoryRepository history,
        IEnumerable<IMongoMigration> migrations,
        ILoggerFactory loggerFactory)
    {
        _database = database;
        _history = history;
        // Ordinal sort on Id keeps the apply order deterministic across cultures.
        _migrations = migrations.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<MongoMigrationRunner>();
    }

    public async Task<int> ApplyPendingAsync(CancellationToken ct)
    {
        var applied = (await _history.GetAppliedAsync(ct))
            .Select(r => r.Id)
            .ToHashSet(StringComparer.Ordinal);

        var pending = _migrations.Where(m => !applied.Contains(m.Id)).ToList();

        if (pending.Count == 0)
        {
            _logger.LogInformation("No pending Mongo migrations to apply.");
            return 0;
        }

        foreach (var migration in pending)
        {
            _logger.LogInformation("Applying Mongo migration {Id} ({Name})", migration.Id, migration.Name);
            var ctx = new MongoMigrationContext(_database, _loggerFactory.CreateLogger(migration.GetType()));
            await migration.UpAsync(ctx, ct);
            await _history.RecordAppliedAsync(migration.Id, migration.Name, ct);
            _logger.LogInformation("Applied Mongo migration {Id}", migration.Id);
        }

        return pending.Count;
    }

    public async Task<bool> RevertLastAsync(CancellationToken ct)
    {
        var latest = await _history.GetLatestAsync(ct);
        if (latest is null)
        {
            _logger.LogInformation("No Mongo migrations to revert.");
            return false;
        }

        var migration = _migrations.SingleOrDefault(m => m.Id == latest.Id)
            ?? throw new InvalidOperationException(
                $"Mongo migration '{latest.Id}' is recorded as applied but is not present in the loaded assembly.");

        _logger.LogInformation("Reverting Mongo migration {Id} ({Name})", migration.Id, migration.Name);
        var ctx = new MongoMigrationContext(_database, _loggerFactory.CreateLogger(migration.GetType()));
        await migration.DownAsync(ctx, ct);
        await _history.RemoveAsync(migration.Id, ct);
        _logger.LogInformation("Reverted Mongo migration {Id}", migration.Id);
        return true;
    }

    public async Task<MongoMigrationStatus> GetStatusAsync(CancellationToken ct)
    {
        var applied = await _history.GetAppliedAsync(ct);
        var appliedIds = applied.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);

        var pending = _migrations
            .Where(m => !appliedIds.Contains(m.Id))
            .Select(m => new MongoMigrationDescriptor(m.Id, m.Name))
            .ToList();

        var appliedDescriptors = applied
            .Select(r => new MongoMigrationDescriptor(r.Id, r.Name))
            .ToList();

        return new MongoMigrationStatus(appliedDescriptors, pending);
    }
}
