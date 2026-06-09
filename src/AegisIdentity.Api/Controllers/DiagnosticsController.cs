using AegisIdentity.SharedKernel.Authorization;
using AegisIdentity.SharedKernel.Constants;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
[Produces("application/json")]
[PermissionGroup(PermissionGroups.Diagnostics)]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly JobStorage _jobStorage;

    public DiagnosticsController(IConnectionMultiplexer redis, JobStorage jobStorage)
    {
        _redis = redis;
        _jobStorage = jobStorage;
    }

    [HttpGet("cache-stats")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Diagnostics.GetCacheStats)]
    [ProducesResponseType(typeof(CacheStatsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCacheStats(CancellationToken ct)
    {
        try
        {
            var server = GetFirstConnectedServer();

            if (server is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var info = await server.InfoAsync("stats");

            var hits   = ExtractLong(info, "keyspace_hits");
            var misses = ExtractLong(info, "keyspace_misses");

            var total = hits + misses;

            if (total == 0)
                return Ok(new CacheStatsResult(HitRate: null));

            var hitRate = (double)hits / total;

            return Ok(new CacheStatsResult(HitRate: hitRate));
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("job-stats")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Diagnostics.GetJobStats)]
    [ProducesResponseType(typeof(JobStatsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetJobStats(CancellationToken ct)
    {
        try
        {
            var monitoring = _jobStorage.GetMonitoringApi();

            var succeededByDate = monitoring.SucceededByDatesCount();

            var dailySeries = succeededByDate
                .OrderBy(kvp => kvp.Key)
                .TakeLast(DashboardSeriesDays)
                .Select(kvp => kvp.Value)
                .ToList();

            DateTime? nextRun = ResolveNextRunFromStorage();

            return Ok(new JobStatsResult(
                DailySeries: dailySeries,
                NextRunUtc: nextRun));
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private DateTime? ResolveNextRunFromStorage()
    {
        try
        {
            using var connection = _jobStorage.GetConnection();

            var hash = connection.GetAllEntriesFromHash($"recurring-job:{JobSchedules.CleanupJobName}");

            if (hash is null || !hash.TryGetValue("NextExecution", out var nextStr))
                return null;

            if (DateTime.TryParse(nextStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var next))
                return next;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private IServer? GetFirstConnectedServer()
        => _redis.GetEndPoints()
                 .Select(ep => _redis.GetServer(ep))
                 .FirstOrDefault(s => s.IsConnected);

    private static long ExtractLong(IGrouping<string, KeyValuePair<string, string>>[] info, string key)
    {
        var entry = info
            .SelectMany(g => g)
            .FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase));

        return long.TryParse(entry.Value, out var value) ? value : 0;
    }

    private const int DashboardSeriesDays = 14;

    public sealed record CacheStatsResult(double? HitRate);

    public sealed record JobStatsResult(
        IReadOnlyList<long> DailySeries,
        DateTime? NextRunUtc);
}
