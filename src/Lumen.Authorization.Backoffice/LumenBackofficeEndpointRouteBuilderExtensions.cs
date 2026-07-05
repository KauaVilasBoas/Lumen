using Lumen.Authorization.Backoffice.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Lumen.Authorization.Backoffice;

/// <summary>
/// Extension methods to mount the Lumen Authorization Backoffice under a configurable prefix.
/// </summary>
public static class LumenBackofficeEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Lumen Authorization Backoffice area routes under <paramref name="prefix"/>.
    /// The default prefix is <c>/lumen</c>.
    /// Ensure the prefix is placed behind the host's authentication middleware — the library
    /// does not provide its own login flow.
    /// </summary>
    public static IEndpointRouteBuilder MapLumenBackoffice(
        this IEndpointRouteBuilder endpoints,
        string prefix = BackofficeRouteDefaults.DefaultPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        endpoints.MapAreaControllerRoute(
            name: "lumen_authz_backoffice",
            areaName: BackofficeRouteDefaults.AreaName,
            pattern: prefix.TrimEnd('/') + "/{controller=Profiles}/{action=Index}/{id?}");

        return endpoints;
    }
}
