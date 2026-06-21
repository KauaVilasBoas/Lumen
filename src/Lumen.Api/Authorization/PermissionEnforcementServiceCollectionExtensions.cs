using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Api.Authorization;

public static class PermissionEnforcementServiceCollectionExtensions
{
    public static IServiceCollection AddPermissionEnforcement(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
