using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Mongo:ConnectionString is required.")]
    public string ConnectionString { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Mongo:Database is required.")]
    public string Database { get; init; } = string.Empty;
}
