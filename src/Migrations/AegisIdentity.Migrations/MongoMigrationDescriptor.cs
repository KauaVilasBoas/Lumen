namespace AegisIdentity.Migrations;

public sealed record MongoMigrationDescriptor(string Id, string Name);

public sealed record MongoMigrationStatus(
    IReadOnlyList<MongoMigrationDescriptor> Applied,
    IReadOnlyList<MongoMigrationDescriptor> Pending);
