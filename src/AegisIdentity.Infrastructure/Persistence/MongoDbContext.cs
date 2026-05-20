using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Persistence.Mappings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace AegisIdentity.Infrastructure.Persistence;

/// <summary>
/// Encapsulates the MongoDB database handle and owns the one-time convention and class-map
/// registration. Thread-safe: <see cref="IMongoDatabase"/> and <see cref="IMongoClient"/>
/// are inherently concurrent; conventions and class maps are registered exactly once via
/// static guards.
/// </summary>
public sealed class MongoDbContext
{
    private static readonly object ConventionLock = new();
    private static bool _conventionsRegistered;

    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        RegisterConventionsOnce();
        RegisterClassMapsOnce();
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

    // ─── BSON class-map registration ─────────────────────────────────────────

    /// <summary>
    /// Delegates class-map registration to each entity's dedicated map class.
    /// Class maps must be registered before any serialization occurs, so this is
    /// called in the constructor alongside convention registration.
    /// </summary>
    private static void RegisterClassMapsOnce()
    {
        UserClassMap.RegisterOnce();
        RefreshTokenClassMap.RegisterOnce();
        PasswordResetTokenClassMap.RegisterOnce();
        EmailConfirmationTokenClassMap.RegisterOnce();
    }
}
