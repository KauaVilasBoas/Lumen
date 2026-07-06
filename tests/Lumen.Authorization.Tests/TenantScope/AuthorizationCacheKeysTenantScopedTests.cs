using FluentAssertions;
using Lumen.Authorization.Internal;

namespace Lumen.Authorization.Tests.TenantScope;

/// <summary>
/// Validates that <see cref="AuthorizationCacheKeys"/> generates distinct, deterministic
/// cache keys for global and scoped entries.
/// </summary>
public sealed class AuthorizationCacheKeysTenantScopedTests
{
    [Fact]
    public void UserPermissions_NullScope_ContainsGlobalSegment()
    {
        var userId = Guid.NewGuid();

        var key = AuthorizationCacheKeys.UserPermissions(userId, scopeId: null);

        key.Should().Contain("global",
            because: "global entries must be identifiable by their cache key segment");
        key.Should().Contain(userId.ToString());
    }

    [Fact]
    public void UserPermissions_WithScope_ContainsScopeId()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();

        var key = AuthorizationCacheKeys.UserPermissions(userId, scopeId);

        key.Should().Contain(scopeId.ToString(),
            because: "scoped entries must embed the scope identifier in the cache key");
        key.Should().Contain(userId.ToString());
    }

    [Fact]
    public void UserPermissions_GlobalAndScoped_ProduceDifferentKeys()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();

        var globalKey = AuthorizationCacheKeys.UserPermissions(userId, null);
        var scopedKey = AuthorizationCacheKeys.UserPermissions(userId, scopeId);

        globalKey.Should().NotBe(scopedKey,
            because: "global and scoped permission caches must not collide");
    }

    [Fact]
    public void UserPermissions_TwoDistinctScopes_ProduceDifferentKeys()
    {
        var userId = Guid.NewGuid();
        var scopeA = Guid.NewGuid();
        var scopeB = Guid.NewGuid();

        var keyA = AuthorizationCacheKeys.UserPermissions(userId, scopeA);
        var keyB = AuthorizationCacheKeys.UserPermissions(userId, scopeB);

        keyA.Should().NotBe(keyB,
            because: "two different scopes must produce distinct cache keys");
    }

    [Fact]
    public void UserPermissions_SameInputs_ProduceSameKey()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();

        var key1 = AuthorizationCacheKeys.UserPermissions(userId, scopeId);
        var key2 = AuthorizationCacheKeys.UserPermissions(userId, scopeId);

        key1.Should().Be(key2,
            because: "cache key generation must be deterministic for the same inputs");
    }

    [Fact]
    public void UserPermissions_DefaultOverload_MatchesNullScopeOverload()
    {
        var userId = Guid.NewGuid();

        var withDefault = AuthorizationCacheKeys.UserPermissions(userId);
        var withNullExplicit = AuthorizationCacheKeys.UserPermissions(userId, null);

        withDefault.Should().Be(withNullExplicit,
            because: "the default (no-scope) overload must produce the same key as null scope");
    }
}
