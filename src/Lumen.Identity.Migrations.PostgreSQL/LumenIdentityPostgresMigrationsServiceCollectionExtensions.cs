using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Identity.Migrations.PostgreSQL;

/// <summary>
/// DI extensions for the Lumen.Identity PostgreSQL migrations assembly.
/// </summary>
public static class LumenIdentityPostgresMigrationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers a hosted service that applies Lumen.Identity EF Core migrations
    /// on startup when targeting PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Equivalent to <c>AddLumenIdentityMigrations()</c> from the SQL Server package.
    /// Call this only when the configured provider is PostgreSQL.
    /// </remarks>
    public static IServiceCollection AddLumenIdentityPostgresMigrations(
        this IServiceCollection services)
    {
        services.AddHostedService<LumenIdentityPostgresMigrationsHostedService>();
        return services;
    }
}
