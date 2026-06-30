using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Modules.Identity.Migrations;

public static class IdentityMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityMigrationsHostedService(this IServiceCollection services)
    {
        services.AddHostedService<IdentityMigrationsHostedService>();
        return services;
    }
}
