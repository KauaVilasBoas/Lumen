using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Modularity;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddLogging();
        services.AddSingleton<IEventBus, InProcessEventBus>();

        RegisterHandlers(services, assemblies);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>);

        var handlerRegistrations = assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type
                .GetInterfaces()
                .Where(iface =>
                    iface.IsGenericType &&
                    iface.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(iface => new { HandlerType = type, ServiceType = iface }));

        foreach (var registration in handlerRegistrations)
        {
            services.AddScoped(registration.ServiceType, registration.HandlerType);
        }
    }
}
