using MongoDB.Driver;

namespace AegisIdentity.Migrations;

public sealed class MongoMigrationHistoryRepository
{
    public const string CollectionName = "__migrations";

    private readonly IMongoCollection<MongoMigrationHistoryRecord> _collection;

    public MongoMigrationHistoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<MongoMigrationHistoryRecord>(CollectionName);
    }

    public async Task<IReadOnlyList<MongoMigrationHistoryRecord>> GetAppliedAsync(CancellationToken ct)
    {
        var records = await _collection
            .Find(FilterDefinition<MongoMigrationHistoryRecord>.Empty)
            .SortBy(r => r.Id)
            .ToListAsync(ct);
        return records;
    }

    public Task<MongoMigrationHistoryRecord?> GetLatestAsync(CancellationToken ct)
        => _collection
            .Find(FilterDefinition<MongoMigrationHistoryRecord>.Empty)
            .SortByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct)!;

    public Task RecordAppliedAsync(string id, string name, CancellationToken ct)
        => _collection.InsertOneAsync(
            new MongoMigrationHistoryRecord
            {
                Id = id,
                Name = name,
                AppliedAt = DateTime.UtcNow,
            },
            cancellationToken: ct);

    public Task RemoveAsync(string id, CancellationToken ct)
        => _collection.DeleteOneAsync(r => r.Id == id, ct);
}
