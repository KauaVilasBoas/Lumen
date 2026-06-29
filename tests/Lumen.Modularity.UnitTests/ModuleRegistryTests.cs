using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lumen.Modularity;

namespace Lumen.Modularity.UnitTests;

public sealed class ModuleRegistryTests
{
    [Fact]
    public void DiscoverModules_WhenAssemblyContainsModuleWithAttribute_ReturnsModule()
    {
        var assemblies = new[] { typeof(ValidModule).Assembly };

        var modules = ModuleRegistry.DiscoverModules(assemblies);

        modules.Should().ContainSingle(m => m.GetType() == typeof(ValidModule));
    }

    [Fact]
    public void DiscoverModules_WhenTypeHasAttributeButDoesNotImplementIModule_IsNotDiscovered()
    {
        var assemblies = new[] { typeof(AttributeOnlyClass).Assembly };

        var modules = ModuleRegistry.DiscoverModules(assemblies);

        modules.Should().NotContain(m => m.GetType() == typeof(AttributeOnlyClass));
    }

    [Fact]
    public void DiscoverModules_WhenTypeImplementsIModuleButLacksAttribute_IsNotDiscovered()
    {
        var assemblies = new[] { typeof(ModuleWithoutAttribute).Assembly };

        var modules = ModuleRegistry.DiscoverModules(assemblies);

        modules.Should().NotContain(m => m.GetType() == typeof(ModuleWithoutAttribute));
    }

    [Fact]
    public void DiscoverModules_WhenMultipleModulesExist_ReturnsAll()
    {
        var assemblies = new[] { typeof(ValidModule).Assembly };

        var modules = ModuleRegistry.DiscoverModules(assemblies);

        modules.Should().Contain(m => m.GetType() == typeof(ValidModule));
        modules.Should().Contain(m => m.GetType() == typeof(AnotherValidModule));
    }

    [Fact]
    public void DiscoverModules_WhenAbstractModuleExists_IsNotDiscovered()
    {
        var assemblies = new[] { typeof(AbstractModule).Assembly };

        var modules = ModuleRegistry.DiscoverModules(assemblies);

        modules.Should().NotContain(m => m.GetType() == typeof(AbstractModule));
    }

    [Fact]
    public void DiscoverModules_WhenNoAssembliesProvided_ReturnsEmpty()
    {
        var modules = ModuleRegistry.DiscoverModules(Array.Empty<Assembly>());

        modules.Should().BeEmpty();
    }
}

[Module]
public sealed class ValidModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}

[Module]
public sealed class AnotherValidModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}

public sealed class ModuleWithoutAttribute : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder endpoints) { }
}

[Module]
public abstract class AbstractModule : IModule
{
    public abstract void RegisterServices(IServiceCollection services, IConfiguration configuration);
    public abstract void MapEndpoints(IEndpointRouteBuilder endpoints);
}

[Module]
public sealed class AttributeOnlyClass;
