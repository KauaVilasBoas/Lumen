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

        services.AddSqlServerOptions(configuration);

        // Adapter bridges AppOptions to the Application-layer abstraction.
        services.AddSingleton<IAppSettings, AppSettingsAdapter>();

        return services;
    }

    /// <summary>
    /// Registers and validates only <see cref="SqlServerOptions"/>.
    ///
    /// Hosts that consume the data-access layer but not the full API stack — e.g.
    /// the Backoffice, which only needs SQL Server (+ Redis via AddRedisCache) — call
    /// this instead of <see cref="AddInfrastructureOptions"/> so they don't fail
    /// startup validation on options they never use (Jwt, Smtp, Hibp, App).
    /// </summary>
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
