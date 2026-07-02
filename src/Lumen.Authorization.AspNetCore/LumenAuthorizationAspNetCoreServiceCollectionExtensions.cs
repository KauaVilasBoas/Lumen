using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Authorization.AspNetCore;

public static class LumenAuthorizationAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddLumenAuthorizationEnforcement(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
