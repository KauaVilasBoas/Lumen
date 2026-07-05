using System.Security.Claims;

namespace Lumen.Authorization;

public sealed class LumenAuthorizationOptions
{
    /// <summary>
    /// Provider de banco de dados usado pelo núcleo de autorização Lumen.
    /// Padrão: <see cref="DatabaseProvider.SqlServer"/>.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    public string? RedisConnectionString { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;
}
