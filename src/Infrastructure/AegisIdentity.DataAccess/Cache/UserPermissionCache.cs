using System.Text.Json;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.DataAccess.Cache;

/// <summary>
/// Distributed-cache implementation of <see cref="IUserPermissionCache"/> backed by Redis.
///
/// Serialization: the permission code set is stored as a JSON array.
/// Resiliency: any exception from the Redis client is caught and logged; callers receive
/// <c>null</c> from <see cref="GetAsync"/> so that upstream authorization can fall back
/// to the database (AUTH-11) without surfacing a 5xx to the end user.
/// </summary>
internal sealed class UserPermissionCache : IUserPermissionCache
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<UserPermissionCache> _logger;

    public UserPermissionCache(IDistributedCache cache, ILogger<UserPermissionCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<HashSet<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(CacheKeys.UserPermissions(userId), cancellationToken);

            if (json is null)
                return null;

            return JsonSerializer.Deserialize<HashSet<string>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while reading user permission cache for {UserId}. Treating as cache miss.", userId);
            return null;
        }
    }

    public async Task SetAsync(Guid userId, HashSet<string> codes, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(codes);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = IUserPermissionCache.DefaultTtl,
            };

            await _cache.SetStringAsync(CacheKeys.UserPermissions(userId), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while writing user permission cache for {UserId}. Continuing without cache.", userId);
        }
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(CacheKeys.UserPermissions(userId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while invalidating user permission cache for {UserId}. Entry will expire via TTL.", userId);
        }
    }
}
