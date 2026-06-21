using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Migrations;

public static class EfMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddEfMigrationsHostedService(this IServiceCollection services)
    {
        services.AddHostedService<EfMigrationsHostedService>();
        return services;
    }
}
