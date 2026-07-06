namespace Lumen.Identity.Persistence;

/// <summary>
/// Marker used to reference the SQL Server migrations assembly name at runtime.
/// The actual assembly name is resolved at DI registration time via
/// <c>LumenIdentityServiceCollectionExtensions</c>.
/// </summary>
internal static class IdentityMigrationsAssemblyNames
{
    public const string SqlServer = "Lumen.Identity.Migrations";
    public const string PostgreSQL = "Lumen.Identity.Migrations.PostgreSQL";
}
