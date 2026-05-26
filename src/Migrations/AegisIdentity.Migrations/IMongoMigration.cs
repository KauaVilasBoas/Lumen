namespace AegisIdentity.Migrations;

public interface IMongoMigration
{
    string Id { get; }

    string Name { get; }

    Task UpAsync(MongoMigrationContext context, CancellationToken cancellationToken);

    Task DownAsync(MongoMigrationContext context, CancellationToken cancellationToken);
}
