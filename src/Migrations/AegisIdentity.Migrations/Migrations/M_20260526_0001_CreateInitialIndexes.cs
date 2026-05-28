using MongoDB.Bson;
using MongoDB.Driver;

namespace AegisIdentity.Migrations.Migrations;

// Migrations intentionally target IMongoCollection<BsonDocument> with raw field
// names so they remain valid even if the corresponding domain entities or
// ClassMaps are later renamed/removed. Frozen-in-time by design.
public sealed class M_20260526_0001_CreateInitialIndexes : IMongoMigration
{
    public string Id => "20260526_0001";

    public string Name => "CreateInitialIndexes";

    public async Task UpAsync(MongoMigrationContext context, CancellationToken ct)
    {
        await CreateUsersIndexesAsync(context, ct);
        await CreateRefreshTokenIndexesAsync(context, ct);
        await CreatePasswordResetTokenIndexesAsync(context, ct);
        await CreateEmailConfirmationTokenIndexesAsync(context, ct);
    }

    public async Task DownAsync(MongoMigrationContext context, CancellationToken ct)
    {
        await DropIndexAsync(context, "users", "ix_email_unique", ct);
        await DropIndexAsync(context, "users", "ix_username_unique", ct);
        await DropIndexAsync(context, "users", "ix_lockedUntil_sparse", ct);

        await DropIndexAsync(context, "refresh_tokens", "ix_tokenHash_unique", ct);
        await DropIndexAsync(context, "refresh_tokens", "ix_userId", ct);
        await DropIndexAsync(context, "refresh_tokens", "ix_expiresAt_ttl", ct);

        await DropIndexAsync(context, "password_reset_tokens", "ix_tokenHash_unique", ct);
        await DropIndexAsync(context, "password_reset_tokens", "ix_expiresAt_ttl", ct);

        await DropIndexAsync(context, "email_confirmation_tokens", "ix_tokenHash_unique", ct);
        await DropIndexAsync(context, "email_confirmation_tokens", "ix_expiresAt_ttl", ct);
    }

    private static async Task CreateUsersIndexesAsync(MongoMigrationContext context, CancellationToken ct)
    {
        var collection = context.Database.GetCollection<BsonDocument>("users");
        var models = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("email"),
                new CreateIndexOptions { Unique = true, Name = "ix_email_unique" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("username"),
                new CreateIndexOptions { Unique = true, Name = "ix_username_unique" }),
            // Sparse: LockedUntil is null for most users; a full index would waste memory.
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("lockedUntil"),
                new CreateIndexOptions { Sparse = true, Name = "ix_lockedUntil_sparse" }),
        };

        await collection.Indexes.CreateManyAsync(models, ct);
    }

    private static async Task CreateRefreshTokenIndexesAsync(MongoMigrationContext context, CancellationToken ct)
    {
        var collection = context.Database.GetCollection<BsonDocument>("refresh_tokens");
        var models = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("userId"),
                new CreateIndexOptions { Name = "ix_userId" }),
            // TTL: Mongo deletes documents once expiresAt <= now.
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        await collection.Indexes.CreateManyAsync(models, ct);
    }

    private static async Task CreatePasswordResetTokenIndexesAsync(MongoMigrationContext context, CancellationToken ct)
    {
        var collection = context.Database.GetCollection<BsonDocument>("password_reset_tokens");
        var models = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        await collection.Indexes.CreateManyAsync(models, ct);
    }

    private static async Task CreateEmailConfirmationTokenIndexesAsync(MongoMigrationContext context, CancellationToken ct)
    {
        var collection = context.Database.GetCollection<BsonDocument>("email_confirmation_tokens");
        var models = new[]
        {
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                new CreateIndexOptions { Unique = true, Name = "ix_tokenHash_unique" }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ix_expiresAt_ttl" }),
        };

        await collection.Indexes.CreateManyAsync(models, ct);
    }

    private static async Task DropIndexAsync(MongoMigrationContext context, string collectionName, string indexName, CancellationToken ct)
    {
        var collection = context.Database.GetCollection<BsonDocument>(collectionName);
        try
        {
            await collection.Indexes.DropOneAsync(indexName, ct);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexNotFound")
        {
            // Down is idempotent — index already absent.
        }
    }
}
