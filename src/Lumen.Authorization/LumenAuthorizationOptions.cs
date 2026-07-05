using System.Security.Claims;

namespace Lumen.Authorization;

public sealed class LumenAuthorizationOptions
{
    public string? RedisConnectionString { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;
}
