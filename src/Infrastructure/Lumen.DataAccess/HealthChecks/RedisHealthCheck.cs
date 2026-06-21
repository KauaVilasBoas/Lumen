using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lumen.DataAccess.HealthChecks;

/// <summary>
/// Health check that probes Redis by writing and removing a sentinel key via
/// <see cref="IDistributedCache"/>, keeping the check consistent with the cache
/// abstraction used throughout the application.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private const string SentinelKey = "health:redis:ping";

    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10),
            };

            await _cache.SetStringAsync(SentinelKey, "1", options, cancellationToken);
            await _cache.RemoveAsync(SentinelKey, cancellationToken);

            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection failed.", exception: ex);
        }
    }
}
