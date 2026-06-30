using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Modularity;

public static class ModularityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        var modules = endpoints.ServiceProvider.GetRequiredService<IReadOnlyList<IModule>>();

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }
}
