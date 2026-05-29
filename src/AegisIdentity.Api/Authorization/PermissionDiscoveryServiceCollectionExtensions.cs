using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Api.Authorization;

public static class PermissionDiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddPermissionDiscovery(this IServiceCollection services)
    {
        services.AddSingleton<PermissionDiscoveryScanner>();
        services.AddScoped<PermissionSyncService>();
        services.AddHostedService<PermissionDiscoveryHostedService>();

        return services;
    }
}
