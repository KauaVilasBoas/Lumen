using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence;

/// <summary>
/// Encapsulates the MongoDB database handle and owns the one-time convention registration.
/// Thread-safe: <see cref="IMongoDatabase"/> and <see cref="IMongoClient"/> are inherently
/// concurrent; the convention pack is registered exactly once via a static guard.
/// </summary>
public sealed class MongoDbContext
{
    private static readonly object ConventionLock = new();
    private static bool _conventionsRegistered;

    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        RegisterConventionsOnce();
        _database = client.GetDatabase(options.Value.Database);
    }

    /// <summary>
    /// Returns a typed collection handle. Callers are responsible for using the correct
    /// collection name for each document type.
    /// </summary>
    public IMongoCollection<TDocument> GetCollection<TDocument>(string name)
        => _database.GetCollection<TDocument>(name);

    /// <summary>
    /// Exposes the underlying database for operations that do not fit the collection abstraction
    /// (e.g., running arbitrary commands or creating indexes via the index manager).
    /// </summary>
    public IMongoDatabase Database => _database;

    // ─── Convention registration ─────────────────────────────────────────────

    private static void RegisterConventionsOnce()
    {
        if (_conventionsRegistered) return;

        lock (ConventionLock)
        {
            if (_conventionsRegistered) return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
            };

            ConventionRegistry.Register("AegisIdentityDefaults", pack, _ => true);
            _conventionsRegistered = true;
        }
    }
}
