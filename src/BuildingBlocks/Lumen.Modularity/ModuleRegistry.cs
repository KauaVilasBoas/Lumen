using System.Reflection;

namespace Lumen.Modularity;

internal static class ModuleRegistry
{
    internal static IReadOnlyList<IModule> DiscoverModules(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.IsDefined(typeof(ModuleAttribute), inherit: false) &&
                typeof(IModule).IsAssignableFrom(type))
            .Select(Activator.CreateInstance)
            .Cast<IModule>()
            .ToList();
    }
}
