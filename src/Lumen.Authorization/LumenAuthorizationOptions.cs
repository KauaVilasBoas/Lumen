namespace Lumen.Authorization;

public sealed class LumenAuthorizationOptions
{
    public string? RedisConnectionString { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; } = true;
}
