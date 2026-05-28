using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AegisIdentity.Migrations;

public sealed class MongoMigrationContext
{
    public MongoMigrationContext(IMongoDatabase database, ILogger logger)
    {
        Database = database;
        Logger = logger;
    }

    public IMongoDatabase Database { get; }

    public ILogger Logger { get; }
}
