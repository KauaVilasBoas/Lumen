using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Infrastructure.Configuration;

/// <summary>
/// Extension methods for registering and validating infrastructure-level configuration options.
/// Call AddInfrastructureOptions(configuration) from Program.cs during service registration.
/// </summary>
public static class InfrastructureOptionsExtensions
{
    /// <summary>
    /// Binds and validates all infrastructure configuration options at application startup.
    /// An <see cref="OptionsValidationException"/> is thrown before the app begins handling
    /// requests if any required value is missing or fails a constraint.
    /// </summary>
    public static IServiceCollection AddInfrastructureOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

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

        return services;
    }
}
