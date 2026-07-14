using Lumen.Authorization.Contracts;
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
        services.AddLumenAuthorizationStartup();

        return services;
    }

    public static IServiceCollection AddLumenAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LumenAuthorizationOptions>? configure = null)
    {
        CoreExtensions.AddLumenAuthorization(services, configuration, configure);
        services.AddLumenAuthorizationEnforcement();
        services.AddLumenAuthorizationStartup();

        return services;
    }

    public static IServiceCollection AddLumenAuthorizationEnforcement(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.TryAddSingleton<IUserIdAccessor, ClaimsUserIdAccessor>();

        return services;
    }

    public static IServiceCollection AddLumenAuthorizationStartup(this IServiceCollection services)
    {
        services.AddSingleton<PermissionDiscoveryScanner>();
        services.AddHostedService<LumenAuthorizationStartupService>();

        return services;
    }

    [Obsolete("Use AddLumenAuthorizationStartup() instead. AddLumenAuthorizationDiscovery() registers only the Sync-mode hosted service without respecting CatalogMode. Will be removed in a future version.")]
    public static IServiceCollection AddLumenAuthorizationDiscovery(this IServiceCollection services)
        => services.AddLumenAuthorizationStartup();

    [Obsolete("Use AddLumenAuthorization() which includes migrations via the unified startup service. AddLumenAuthorizationMigrations() is now a no-op; migrations are applied by LumenAuthorizationStartupService.")]
    public static IServiceCollection AddLumenAuthorizationMigrations(this IServiceCollection services)
        => services;
}
