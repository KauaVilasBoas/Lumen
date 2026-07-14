using FluentAssertions;
using Lumen.Authorization;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.AspNetCore.Tests.ServiceRegistration;

public sealed class AddLumenAuthorizationWebTests
{
    private const string FakeSqlConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;";
    private const string FakeRedisConnectionString = "localhost:6379";

    private static IServiceCollection BuildMinimalServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        return services;
    }

    private static IConfiguration BuildConfiguration(string connectionString, string? redisConnectionString = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString
        };

        if (redisConnectionString is not null)
            values["ConnectionStrings:Redis"] = redisConnectionString;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public void StringOverload_RegistersCoreServices()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        services.Should().Contain(d => d.ServiceType == typeof(IUserPermissionService),
            because: "the umbrella must register the core IUserPermissionService");
    }

    [Fact]
    public void StringOverload_RegistersDistributedCache()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        services.Should().Contain(d => d.ServiceType == typeof(IDistributedCache),
            because: "the umbrella must register a distributed cache");
    }

    [Fact]
    public void StringOverload_RegistersStartupHostedService()
    {
        var services = BuildMinimalServices();
        services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>(
            _ => new FakeActionDescriptorCollectionProvider());

        services.AddLumenAuthorization(FakeSqlConnectionString);

        services.Should().Contain(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(LumenAuthorizationStartupService),
            because: "the umbrella must register the unified startup hosted service");
    }

    [Fact]
    public void StringOverload_RegistersPolicyProvider()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        services.Should().Contain(d => d.ServiceType == typeof(IAuthorizationPolicyProvider),
            because: "the umbrella must register the permission policy provider");
    }

    [Fact]
    public void StringOverload_RegistersAuthorizationHandler()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        services.Should().Contain(d => d.ServiceType == typeof(IAuthorizationHandler),
            because: "the umbrella must register the permission authorization handler");
    }

    [Fact]
    public void StringOverload_RegistersUserIdAccessor()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        services.Should().Contain(d => d.ServiceType == typeof(IUserIdAccessor),
            because: "the umbrella must register IUserIdAccessor");
    }

    [Fact]
    public void StringOverload_ConfigureDelegate_PropagatedToOptions()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString, o =>
        {
            o.RedisConnectionString = FakeRedisConnectionString;
        });

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<LumenAuthorizationOptions>>()
            .Value;

        options.RedisConnectionString.Should().Be(FakeRedisConnectionString,
            because: "the configure delegate must be forwarded to the core registration");
    }

    [Fact]
    public void StringOverload_DefaultCatalogMode_IsValidate()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<LumenAuthorizationOptions>>()
            .Value;

        options.CatalogMode.Should().Be(PermissionCatalogMode.Validate,
            because: "the default CatalogMode must be Validate to avoid writing to the consumer's catalog");
    }

    [Fact]
    public void StringOverload_DefaultFailFast_IsFalse()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<LumenAuthorizationOptions>>()
            .Value;

        options.FailFastOnMissingPermission.Should().BeFalse(
            because: "the default must be to log a warning rather than abort startup");
    }

    [Fact]
    public void StringOverload_DefaultAutoGrantAllToAdministrator_IsFalse()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<LumenAuthorizationOptions>>()
            .Value;

        options.AutoGrantAllToAdministrator.Should().BeFalse(
            because: "auto-granting all permissions to Administrator must be opt-in");
    }

    [Fact]
    public void StringOverload_ConfigureDelegate_PropagatesCatalogOptions()
    {
        var services = BuildMinimalServices();

        services.AddLumenAuthorization(FakeSqlConnectionString, o =>
        {
            o.CatalogMode = PermissionCatalogMode.Sync;
            o.FailFastOnMissingPermission = true;
            o.AutoGrantAllToAdministrator = true;
        });

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<LumenAuthorizationOptions>>()
            .Value;

        options.CatalogMode.Should().Be(PermissionCatalogMode.Sync);
        options.FailFastOnMissingPermission.Should().BeTrue();
        options.AutoGrantAllToAdministrator.Should().BeTrue();
    }

    [Fact]
    public void IConfigurationOverload_RegistersCoreServices()
    {
        var services = BuildMinimalServices();
        var configuration = BuildConfiguration(FakeSqlConnectionString);

        services.AddLumenAuthorization(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(IUserPermissionService),
            because: "the IConfiguration umbrella overload must register the core IUserPermissionService");
    }

    [Fact]
    public void IConfigurationOverload_RegistersStartupHostedService()
    {
        var services = BuildMinimalServices();
        services.AddSingleton<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>(
            _ => new FakeActionDescriptorCollectionProvider());
        var configuration = BuildConfiguration(FakeSqlConnectionString);

        services.AddLumenAuthorization(configuration);

        services.Should().Contain(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(LumenAuthorizationStartupService),
            because: "the IConfiguration umbrella overload must register the unified startup hosted service");
    }

    [Fact]
    public void IConfigurationOverload_RegistersPolicyProviderAndHandler()
    {
        var services = BuildMinimalServices();
        var configuration = BuildConfiguration(FakeSqlConnectionString);

        services.AddLumenAuthorization(configuration);

        services.Should().Contain(d => d.ServiceType == typeof(IAuthorizationPolicyProvider),
            because: "the IConfiguration umbrella overload must register the permission policy provider");

        services.Should().Contain(d => d.ServiceType == typeof(IAuthorizationHandler),
            because: "the IConfiguration umbrella overload must register the permission authorization handler");
    }

    [Fact]
    public void IConfigurationOverload_ReadsRedisFromConfiguration()
    {
        var services = BuildMinimalServices();
        var configuration = BuildConfiguration(FakeSqlConnectionString, FakeRedisConnectionString);

        services.AddLumenAuthorization(configuration);

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));

        cacheDescriptor.Should().NotBeNull();
        cacheDescriptor!.ImplementationType!.Name.Should().Contain("Redis",
            because: "when ConnectionStrings:Redis is present, the Redis provider must be registered");
    }

    private sealed class FakeActionDescriptorCollectionProvider
        : Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider
    {
        public Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors =>
            new([], version: 0);
    }
}
