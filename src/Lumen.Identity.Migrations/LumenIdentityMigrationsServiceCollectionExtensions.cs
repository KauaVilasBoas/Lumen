using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Identity.Migrations;

public static class LumenIdentityMigrationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers a hosted service that applies Lumen.Identity EF Core migrations on startup.
    /// </summary>
    public static IServiceCollection AddLumenIdentityMigrations(this IServiceCollection services)
    {
        services.AddHostedService<LumenIdentityMigrationsHostedService>();
        return services;
    }
}
