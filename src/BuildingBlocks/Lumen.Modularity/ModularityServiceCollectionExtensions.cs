using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Modularity;

public static class ModularityServiceCollectionExtensions
{
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var modules = ModuleRegistry.DiscoverModules(assemblies);

        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
        }

        services.AddSingleton<IReadOnlyList<IModule>>(modules);

        return services;
    }
}
