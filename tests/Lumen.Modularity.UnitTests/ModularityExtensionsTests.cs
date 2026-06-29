using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lumen.Modularity;

namespace Lumen.Modularity.UnitTests;

public sealed class ModularityExtensionsTests
{
    [Fact]
    public void AddModules_RegistersDiscoveredModulesInDI()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddModules(configuration, typeof(ValidModule).Assembly);
        var provider = services.BuildServiceProvider();

        var modules = provider.GetRequiredService<IReadOnlyList<IModule>>();
        modules.Should().Contain(m => m.GetType() == typeof(ValidModule));
    }

    [Fact]
    public void AddModules_CallsRegisterServicesOnEachModule()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddModules(configuration, typeof(ServiceRegisteringModule).Assembly);
        var provider = services.BuildServiceProvider();

        provider.GetService<SentinelService>().Should().NotBeNull();
    }

    [Fact]
    public void AddEventBus_RegistersIEventBusAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEventBus(typeof(OrderEventHandler).Assembly);
        var provider = services.BuildServiceProvider();

        var bus = provider.GetService<IEventBus>();
        bus.Should().NotBeNull().And.BeOfType<InProcessEventBus>();
    }

    [Fact]
    public void AddEventBus_RegistersHandlersFoundInAssemblies()
    {
        var services = new ServiceCollection();

        services.AddEventBus(typeof(OrderEventHandler).Assembly);
        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<IIntegrationEventHandler<OrderPlacedEvent>>();
        handler.Should().NotBeNull();
    }
}

[Module]
public sealed class ServiceRegisteringModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SentinelService>();
    }

    public void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { }
}

public sealed class SentinelService;

public sealed class OrderEventHandler : IIntegrationEventHandler<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent integrationEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
