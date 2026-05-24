using AegisIdentity.DataAccess.Persistence.Mappings;
using AegisIdentity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace AegisIdentity.DataAccess.Persistence;

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

    public IMongoCollection<TDocument> GetCollection<TDocument>(string name)
        => _database.GetCollection<TDocument>(name);

    public IMongoDatabase Database => _database;

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

    private static void RegisterClassMapsOnce()
    {
        UserClassMap.RegisterOnce();
        RefreshTokenClassMap.RegisterOnce();
        PasswordResetTokenClassMap.RegisterOnce();
        EmailConfirmationTokenClassMap.RegisterOnce();
    }
}
