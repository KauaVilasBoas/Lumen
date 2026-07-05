using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Authorization.Migrations;

public static class LumenAuthorizationMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddLumenAuthorizationMigrations(this IServiceCollection services)
    {
        services.AddHostedService<LumenAuthorizationMigrationsHostedService>();
        return services;
    }
}
