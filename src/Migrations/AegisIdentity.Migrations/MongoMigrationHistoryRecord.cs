using MongoDB.Bson.Serialization.Attributes;

namespace AegisIdentity.Migrations;

public sealed class MongoMigrationHistoryRecord
{
    [BsonId]
    public string Id { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("appliedAt")]
    public DateTime AppliedAt { get; set; }
}
