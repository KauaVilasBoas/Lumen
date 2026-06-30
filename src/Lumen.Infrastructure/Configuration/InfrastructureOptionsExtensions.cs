using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Infrastructure.Configuration;

public static class InfrastructureOptionsExtensions
{
    public static IServiceCollection AddInfrastructureOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction = false)
    {
        AddSqlServerOptions(services, configuration);
        return services;
    }

    public static IServiceCollection AddSqlServerOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SqlServerOptions>()
            .Bind(configuration.GetSection(SqlServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
