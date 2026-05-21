using Microsoft.Extensions.DependencyInjection;

namespace AegisIdentity.Application.Security;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddApplicationSecurity(this IServiceCollection services)
    {
        services.AddScoped<IPasswordValidator, PasswordValidator>();
        return services;
    }
}
