using Lumen.Authorization.Contracts;
using Lumen.Authorization.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CoreExtensions = Lumen.Authorization.LumenAuthorizationServiceCollectionExtensions;

namespace Lumen.Authorization.AspNetCore;

public static class LumenAuthorizationAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        string connectionString,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        CoreExtensions.AddLumenAuthorization(services, connectionString, configure);
        services.AddLumenAuthorizationEnforcement();
        services.AddLumenAuthorizationMigrations();

        return services;
    }

    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        CoreExtensions.AddLumenAuthorization(services, configuration, configure);
        services.AddLumenAuthorizationEnforcement();
        services.AddLumenAuthorizationMigrations();

        return services;
    }

    public static IServiceCollection AddLumenAuthorizationEnforcement(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.TryAddSingleton<IUserIdAccessor, ClaimsUserIdAccessor>();

        return services;
    }
}
