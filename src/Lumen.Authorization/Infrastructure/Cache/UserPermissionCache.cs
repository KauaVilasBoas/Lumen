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

    public async Task<HashSet<string>?> GetAsync(Guid userId, Guid? scopeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = AuthorizationCacheKeys.UserPermissions(userId, scopeId);
            var json = await _cache.GetStringAsync(key, cancellationToken);

            if (json is null)
                return null;

            return JsonSerializer.Deserialize<HashSet<string>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache unavailable while reading permission cache for {UserId} scope {ScopeId}.", userId, scopeId);
            return null;
        }
    }

    public async Task SetAsync(Guid userId, Guid? scopeId, HashSet<string> codes, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = AuthorizationCacheKeys.UserPermissions(userId, scopeId);
            var json = JsonSerializer.Serialize(codes);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = IUserPermissionCache.DefaultTtl,
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache unavailable while writing permission cache for {UserId} scope {ScopeId}.", userId, scopeId);
        }
    }

    public async Task InvalidateAsync(Guid userId, Guid? scopeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = AuthorizationCacheKeys.UserPermissions(userId, scopeId);
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to invalidate permission cache for user {UserId} scope {ScopeId}. Propagating exception.",
                userId,
                scopeId);
            throw;
        }
    }
}
