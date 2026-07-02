using FluentAssertions;
using Lumen.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.Tests.ServiceRegistration;

public sealed class AddLumenAuthorizationTests
{
    private const string FakeSqlConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;";
    private const string FakeRedisConnectionString = "localhost:6379";

    [Fact]
    public void WithoutRedis_RegistersDistributedMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType!.Name.Should().NotContain("Redis",
            because: "without a Redis connection string, the in-memory provider must be used");
    }

    [Fact]
    public void WithRedisConnectionString_RegistersRedisCacheDescriptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLumenAuthorization(FakeSqlConnectionString, o =>
        {
            o.RedisConnectionString = FakeRedisConnectionString;
        });

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType!.Name.Should().Contain("Redis",
            because: "when RedisConnectionString is set, the Redis provider must be registered");
    }

    [Fact]
    public void WhenConsumerPreRegistersDistributedCache_DoesNotOverwrite()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDistributedCache, MemoryDistributedCache>();

        services.AddLumenAuthorization(FakeSqlConnectionString, o =>
        {
            o.RedisConnectionString = FakeRedisConnectionString;
        });

        var descriptors = services.Where(d => d.ServiceType == typeof(IDistributedCache)).ToList();

        descriptors.Should().HaveCount(1,
            because: "a pre-registered IDistributedCache must not be replaced by AddLumenAuthorization");
        descriptors[0].ImplementationType.Should().Be(typeof(MemoryDistributedCache),
            because: "the consumer's registration must remain intact");
    }

    [Fact]
    public void IConfigurationOverload_MapsDefaultConnectionAndRedis()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = FakeSqlConnectionString,
            ["ConnectionStrings:Redis"] = FakeRedisConnectionString
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLumenAuthorization(configuration);

        var options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<LumenAuthorizationOptions>>()
            .Value;

        options.RedisConnectionString.Should().Be(FakeRedisConnectionString,
            because: "the IConfiguration overload must read ConnectionStrings:Redis into RedisConnectionString");

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));
        cacheDescriptor.Should().NotBeNull();
        cacheDescriptor!.ImplementationType!.Name.Should().Contain("Redis",
            because: "when ConnectionStrings:Redis is present, the Redis provider must be registered");
    }
}
