using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Migrations;

public static class EfMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddEfMigrationsHostedService(this IServiceCollection services)
    {
        services.AddHostedService<EfMigrationsHostedService>();
        return services;
    }
}
