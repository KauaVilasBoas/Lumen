using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Redis:ConnectionString is required.")]
    public string ConnectionString { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Redis:InstanceName is required.")]
    public string InstanceName { get; init; } = "aegis:";
}
