using System.Security.Claims;

namespace Lumen.Authorization;

public sealed class LumenAuthorizationOptions
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    public string? RedisConnectionString { get; set; }

    public bool ApplyMigrationsOnStartup { get; set; } = true;

    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;

    public PermissionCatalogMode CatalogMode { get; set; } = PermissionCatalogMode.Validate;

    public bool FailFastOnMissingPermission { get; set; } = false;

    public bool AutoGrantAllToAdministrator { get; set; } = false;
}
