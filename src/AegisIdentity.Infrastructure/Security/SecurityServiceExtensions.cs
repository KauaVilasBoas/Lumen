using AegisIdentity.Domain.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Infrastructure.Security;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();

        return services;
    }
}
