using System.Text.Json;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Lumen.Authorization.Infrastructure.Cache;

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
            var json = await _cache.GetStringAsync(AuthorizationCacheKeys.UserPermissions(userId), cancellationToken);

            if (json is null)
                return null;

            return JsonSerializer.Deserialize<HashSet<string>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache unavailable while reading permission cache for {UserId}.", userId);
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

            await _cache.SetStringAsync(AuthorizationCacheKeys.UserPermissions(userId), json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache unavailable while writing permission cache for {UserId}.", userId);
        }
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(AuthorizationCacheKeys.UserPermissions(userId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to invalidate permission cache for user {UserId}. Propagating exception.",
                userId);
            throw;
        }
    }
}
