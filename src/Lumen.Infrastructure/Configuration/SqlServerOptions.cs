using System.ComponentModel.DataAnnotations;

namespace AegisIdentity.Infrastructure.Configuration;

public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    [Required(AllowEmptyStrings = false, ErrorMessage = "SqlServer:ConnectionString is required.")]
    public string ConnectionString { get; init; } = string.Empty;
}
