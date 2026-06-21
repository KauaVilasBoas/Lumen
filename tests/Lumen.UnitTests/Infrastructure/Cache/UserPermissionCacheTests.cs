using AegisIdentity.DataAccess.Cache;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisIdentity.UnitTests.Infrastructure.Cache;

/// <summary>
/// Simulates a Redis client that always throws on every operation.
/// Mirrors the <c>BrokenDistributedCache</c> used in integration tests.
/// </summary>
file sealed class AlwaysFailingCache : IDistributedCache
{
    private static Exception Fail() => new InvalidOperationException("Redis is unavailable.");

    public byte[]? Get(string key) => throw Fail();
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Fail();
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Fail();
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => throw Fail();
    public void Refresh(string key) => throw Fail();
    public Task RefreshAsync(string key, CancellationToken token = default) => throw Fail();
    public void Remove(string key) => throw Fail();
    public Task RemoveAsync(string key, CancellationToken token = default) => throw Fail();
}

public sealed class UserPermissionCacheTests
{
    private static UserPermissionCache BuildSut(IDistributedCache cache) =>
        new(cache, NullLogger<UserPermissionCache>.Instance);

    private static IDistributedCache InMemoryCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    // ── InvalidateAsync — fail-closed (FIX-04) ──────────────────────────────

    [Fact]
    public async Task InvalidateAsync_WhenRedisSucceeds_DoesNotThrow()
    {
        var sut = BuildSut(InMemoryCache());
        var userId = Guid.NewGuid();

        var act = async () => await sut.InvalidateAsync(userId, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateAsync_WhenRedisThrows_PropagatesException()
    {
        // Fail-closed contract: a failed invalidation must surface so the caller knows
        // the revocation did not take effect in cache.
        var sut = BuildSut(new AlwaysFailingCache());
        var userId = Guid.NewGuid();

        var act = async () => await sut.InvalidateAsync(userId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Redis is unavailable.");
    }

    // ── GetAsync — fail-open (regression guard) ──────────────────────────────

    [Fact]
    public async Task GetAsync_WhenRedisThrows_ReturnsNullAndDoesNotThrow()
    {
        // Reading from cache is fail-open by design: a cache miss is tolerable because
        // auth falls back to the database.  This test guards that contract does not change.
        var sut = BuildSut(new AlwaysFailingCache());
        var userId = Guid.NewGuid();

        var result = await sut.GetAsync(userId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenEntryExists_ReturnsDeserializedCodes()
    {
        var userId = Guid.NewGuid();
        var codes = new HashSet<string> { "Docs.Read", "Docs.Write" };
        var cache = InMemoryCache();
        var sut = BuildSut(cache);

        await sut.SetAsync(userId, codes, CancellationToken.None);
        var result = await sut.GetAsync(userId, CancellationToken.None);

        result.Should().BeEquivalentTo(codes);
    }

    // ── SetAsync — fail-open (regression guard) ──────────────────────────────

    [Fact]
    public async Task SetAsync_WhenRedisThrows_DoesNotThrow()
    {
        // Writing to cache is fail-open by design: losing the warm entry is tolerable.
        var sut = BuildSut(new AlwaysFailingCache());
        var userId = Guid.NewGuid();
        var codes = new HashSet<string> { "Docs.Read" };

        var act = async () => await sut.SetAsync(userId, codes, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── InvalidateAsync removes entry ─────────────────────────────────────────

    [Fact]
    public async Task InvalidateAsync_WhenEntryExists_RemovesItFromCache()
    {
        var userId = Guid.NewGuid();
        var codes = new HashSet<string> { "Admin.Access" };
        var cache = InMemoryCache();
        var sut = BuildSut(cache);

        await sut.SetAsync(userId, codes, CancellationToken.None);
        await sut.InvalidateAsync(userId, CancellationToken.None);
        var result = await sut.GetAsync(userId, CancellationToken.None);

        result.Should().BeNull("entry was explicitly invalidated and must not be retrievable");
    }
}
