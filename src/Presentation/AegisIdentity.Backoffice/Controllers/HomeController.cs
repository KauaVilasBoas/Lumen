using AegisIdentity.Backoffice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    private readonly AdminApiClient _adminApiClient;

    public HomeController(AdminApiClient adminApiClient) => _adminApiClient = adminApiClient;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userCountTask    = _adminApiClient.GetUserCountAsync(ct);
        var profilesTask     = _adminApiClient.ListProfilesAsync(ct);
        var permissionsTask  = _adminApiClient.ListPermissionsAsync(ct);
        var activityTask     = _adminApiClient.GetRecentActivityAsync(take: 10, ct);
        var cacheStatsTask   = _adminApiClient.GetCacheStatsAsync(ct);
        var jobStatsTask     = _adminApiClient.GetJobStatsAsync(ct);

        try
        {
            await Task.WhenAll(
                userCountTask,
                profilesTask,
                permissionsTask,
                activityTask,
                cacheStatsTask,
                jobStatsTask);
        }
        catch
        {
        }

        var userCount = await SafeResult(userCountTask);
        if (userCount.HasValue) ViewBag.UserCount = userCount.Value;

        var profiles = await SafeResult(profilesTask);
        if (profiles is not null) ViewBag.ProfileCount = profiles.Count;

        var permissionGroups = await SafeResult(permissionsTask);
        if (permissionGroups is not null)
        {
            var all = permissionGroups.SelectMany(g => g.Permissions).ToList();
            ViewBag.PermissionCount = all.Count;
            ViewBag.OrphanCount     = all.Count(p => p.IsOrphan);
        }

        var activity = await SafeResult(activityTask);
        if (activity is not null) ViewBag.Activity = activity;

        var cacheStats = await SafeResult(cacheStatsTask);
        if (cacheStats?.HitRate is not null) ViewBag.CacheHitRate = cacheStats.HitRate;

        var jobStats = await SafeResult(jobStatsTask);
        if (jobStats is not null) ViewBag.JobStats = jobStats;

        return View();
    }

    private static async Task<T?> SafeResult<T>(Task<T?> task) where T : class
    {
        try { return await task; }
        catch { return null; }
    }

    private static async Task<T?> SafeResult<T>(Task<T?> task) where T : struct
    {
        try { return await task; }
        catch { return null; }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Architecture() => View();
}
