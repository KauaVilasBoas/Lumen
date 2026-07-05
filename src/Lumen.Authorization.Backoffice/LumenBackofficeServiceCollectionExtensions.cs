using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Authorization.Backoffice;

/// <summary>
/// Registers the Lumen Authorization Backoffice UI into the consumer's DI container.
/// Call <c>AddLumenAuthorization</c> and <c>AddLumenAuthorizationEnforcement</c> before this method.
/// Authentication (login) is the consumer's responsibility — mount the backoffice route behind
/// the host's authentication middleware.
/// </summary>
public static class LumenBackofficeServiceCollectionExtensions
{
    public static IServiceCollection AddLumenBackoffice(this IServiceCollection services)
    {
        services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(LumenBackofficeServiceCollectionExtensions).Assembly);

        return services;
    }
}
