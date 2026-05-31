using AegisIdentity.Domain.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Infrastructure.Configuration;

public static class InfrastructureOptionsExtensions
{
    public static IServiceCollection AddInfrastructureOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<HibpOptions>()
            .Bind(configuration.GetSection(HibpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<SqlServerOptions>()
            .Bind(configuration.GetSection(SqlServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Adapter bridges AppOptions to the Application-layer abstraction.
        services.AddSingleton<IAppSettings, AppSettingsAdapter>();

        return services;
    }
}
