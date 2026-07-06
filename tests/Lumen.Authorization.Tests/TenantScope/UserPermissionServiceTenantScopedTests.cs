using FluentAssertions;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Infrastructure.Cache;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Authorization.Tests.TenantScope;

/// <summary>
/// Validates that <see cref="UserPermissionService"/> correctly partitions
/// permissions by scope and preserves backward-compatible global behavior.
/// </summary>
public sealed class UserPermissionServiceTenantScopedTests
{
    private readonly IUserPermissionCache _cache = Substitute.For<IUserPermissionCache>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    private UserPermissionService CreateService()
        => new(_cache, _profileRepository, NullLogger<UserPermissionService>.Instance);

    // ── Global path (scopeId = null) — backward-compatible behavior ──────────

    [Fact]
    public async Task GetPermissions_WhenScopeIdIsNull_QueriesGlobalPermissions()
    {
        var userId = Guid.NewGuid();
        var globalCodes = new HashSet<string> { "Users.List", "Users.Create" };

        _cache.GetAsync(userId, null, Arg.Any<CancellationToken>()).Returns((HashSet<string>?)null);
        _profileRepository.GetPermissionCodesByUserIdAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns(globalCodes);

        var service = CreateService();
        var result = await service.GetPermissionsAsync(userId, scopeId: null);

        result.Should().BeEquivalentTo(globalCodes);
        await _profileRepository.Received(1)
            .GetPermissionCodesByUserIdAsync(userId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HasPermission_WhenScopeIdIsNull_UsesGlobalPermissions()
    {
        var userId = Guid.NewGuid();
        _cache.GetAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "Users.List" });

        var service = CreateService();
        var has = await service.HasPermissionAsync(userId, "Users.List", scopeId: null);

        has.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermission_DefaultOverload_BehavesIdenticalToExplicitNullScope()
    {
        var userId = Guid.NewGuid();
        _cache.GetAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "Users.List" });

        var service = CreateService();

        // The default overload (no scopeId arg) must behave as scopeId = null.
        var has = await service.HasPermissionAsync(userId, "Users.List");

        has.Should().BeTrue(
            because: "the default overload must pass scopeId = null, preserving backward compatibility");
    }

    // ── Scoped path — different permissions per tenant ────────────────────────

    [Fact]
    public async Task GetPermissions_WhenScopeIdIsProvided_QueriesScopedPermissions()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();
        var scopedCodes = new HashSet<string> { "Reports.View" };

        _cache.GetAsync(userId, scopeId, Arg.Any<CancellationToken>()).Returns((HashSet<string>?)null);
        _profileRepository.GetPermissionCodesByUserIdAsync(userId, scopeId, Arg.Any<CancellationToken>())
            .Returns(scopedCodes);

        var service = CreateService();
        var result = await service.GetPermissionsAsync(userId, scopeId);

        result.Should().BeEquivalentTo(scopedCodes);
        await _profileRepository.Received(1)
            .GetPermissionCodesByUserIdAsync(userId, scopeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPermissions_SameUserDifferentScopes_ReturnsDifferentPermissions()
    {
        var userId = Guid.NewGuid();
        var scopeA = Guid.NewGuid();
        var scopeB = Guid.NewGuid();

        var codesForA = new HashSet<string> { "Users.Admin" };
        var codesForB = new HashSet<string> { "Reports.View" };

        _cache.GetAsync(userId, scopeA, Arg.Any<CancellationToken>()).Returns((HashSet<string>?)null);
        _cache.GetAsync(userId, scopeB, Arg.Any<CancellationToken>()).Returns((HashSet<string>?)null);
        _profileRepository.GetPermissionCodesByUserIdAsync(userId, scopeA, Arg.Any<CancellationToken>())
            .Returns(codesForA);
        _profileRepository.GetPermissionCodesByUserIdAsync(userId, scopeB, Arg.Any<CancellationToken>())
            .Returns(codesForB);

        var service = CreateService();

        var resultA = await service.GetPermissionsAsync(userId, scopeA);
        var resultB = await service.GetPermissionsAsync(userId, scopeB);

        resultA.Should().BeEquivalentTo(codesForA,
            because: "scope A grants admin permissions");
        resultB.Should().BeEquivalentTo(codesForB,
            because: "scope B grants only reporting permissions");
        resultA.Should().NotBeEquivalentTo(resultB,
            because: "different scopes must yield independent permission sets");
    }

    [Fact]
    public async Task HasPermission_ScopedUserLacksGlobalPermission_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();

        // User has "Users.Admin" in scope but NOT globally.
        _cache.GetAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _cache.GetAsync(userId, scopeId, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "Users.Admin" });

        var service = CreateService();

        var hasGlobally = await service.HasPermissionAsync(userId, "Users.Admin", scopeId: null);
        var hasScopedOnly = await service.HasPermissionAsync(userId, "Users.Admin", scopeId);

        hasGlobally.Should().BeFalse(
            because: "scoped permissions must not bleed into the global context");
        hasScopedOnly.Should().BeTrue();
    }

    // ── Cache partitioning by (userId, scopeId) ───────────────────────────────

    [Fact]
    public async Task GetPermissions_CacheHit_DoesNotQueryDatabase()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();
        var cachedCodes = new HashSet<string> { "Cached.Permission" };

        _cache.GetAsync(userId, scopeId, Arg.Any<CancellationToken>()).Returns(cachedCodes);

        var service = CreateService();
        var result = await service.GetPermissionsAsync(userId, scopeId);

        result.Should().BeEquivalentTo(cachedCodes);
        await _profileRepository.DidNotReceive()
            .GetPermissionCodesByUserIdAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPermissions_CacheMiss_WritesToCacheWithCorrectScopeKey()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();
        var dbCodes = new HashSet<string> { "Orders.Create" };

        _cache.GetAsync(userId, scopeId, Arg.Any<CancellationToken>()).Returns((HashSet<string>?)null);
        _profileRepository.GetPermissionCodesByUserIdAsync(userId, scopeId, Arg.Any<CancellationToken>())
            .Returns(dbCodes);

        var service = CreateService();
        await service.GetPermissionsAsync(userId, scopeId);

        // Must write to cache keyed by the specific scopeId, not the global key.
        await _cache.Received(1)
            .SetAsync(userId, scopeId, dbCodes, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive()
            .SetAsync(userId, null, Arg.Any<HashSet<string>>(), Arg.Any<CancellationToken>());
    }
}
