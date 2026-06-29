using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Modules.Audit.Migrations;

public static class AuditMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddAuditMigrationsHostedService(this IServiceCollection services)
    {
        services.AddHostedService<AuditMigrationsHostedService>();
        return services;
    }
}
