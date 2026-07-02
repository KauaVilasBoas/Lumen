using Lumen.Authorization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lumen.Authorization.AspNetCore;

public static class LumenAuthorizationAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddLumenAuthorizationEnforcement(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.TryAddSingleton<IUserIdAccessor, ClaimsUserIdAccessor>();

        return services;
    }

    public static IServiceCollection AddLumenAuthorizationDiscovery(this IServiceCollection services)
    {
        services.AddSingleton<PermissionDiscoveryScanner>();
        services.AddHostedService<PermissionDiscoveryAndReconciliationHostedService>();

        return services;
    }
}
