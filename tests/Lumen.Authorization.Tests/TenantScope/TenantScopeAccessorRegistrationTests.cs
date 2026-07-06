using FluentAssertions;
using Lumen.Authorization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Authorization.Tests.TenantScope;

/// <summary>
/// Validates that <see cref="ITenantScopeAccessor"/> is registered with the correct
/// default behavior and can be overridden by host implementations.
/// </summary>
public sealed class TenantScopeAccessorRegistrationTests
{
    private const string FakeSqlConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;";

    [Fact]
    public void AddLumenAuthorization_RegistersNoOpTenantScopeAccessorByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddLumenAuthorization(FakeSqlConnectionString);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITenantScopeAccessor));

        descriptor.Should().NotBeNull(
            because: "ITenantScopeAccessor must be registered by AddLumenAuthorization");
    }

    [Fact]
    public void AddLumenAuthorization_DefaultAccessor_ReturnsNullScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLumenAuthorization(FakeSqlConnectionString);

        var sp = services.BuildServiceProvider();
        var accessor = sp.GetRequiredService<ITenantScopeAccessor>();

        accessor.GetCurrentScopeId().Should().BeNull(
            because: "the default no-op accessor must return null (global context) for non-tenant apps");
    }

    [Fact]
    public void AddLumenAuthorization_HostOverridesAccessor_HostImplementationWins()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Host registers its own implementation BEFORE AddLumenAuthorization.
        var expectedScopeId = Guid.NewGuid();
        services.AddScoped<ITenantScopeAccessor>(_ => new FakeTenantScopeAccessor(expectedScopeId));

        services.AddLumenAuthorization(FakeSqlConnectionString);

        var sp = services.BuildServiceProvider();
        var accessor = sp.GetRequiredService<ITenantScopeAccessor>();

        accessor.GetCurrentScopeId().Should().Be(expectedScopeId,
            because: "a host implementation registered before AddLumenAuthorization must take precedence (TryAddScoped)");
    }

    private sealed class FakeTenantScopeAccessor : ITenantScopeAccessor
    {
        private readonly Guid _scopeId;

        public FakeTenantScopeAccessor(Guid scopeId) => _scopeId = scopeId;

        public Guid? GetCurrentScopeId() => _scopeId;
    }
}
