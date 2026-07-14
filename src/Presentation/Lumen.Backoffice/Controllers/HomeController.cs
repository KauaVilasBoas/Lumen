using Lumen.Backoffice.Services;
using Lumen.Backoffice.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Backoffice.Controllers;

[Authorize]
public sealed class HomeController : BackofficeBaseController
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

        var userCount        = await SafeResult(userCountTask);
        var profiles         = await SafeResult(profilesTask);
        var permissionGroups = await SafeResult(permissionsTask);
        var activity         = await SafeResult(activityTask);
        var cacheStats       = await SafeResult(cacheStatsTask);
        var jobStats         = await SafeResult(jobStatsTask);

        int? permissionCount = null;
        if (permissionGroups is not null)
            permissionCount = permissionGroups.SelectMany(g => g.Permissions).Count();

        return View(new HomeDashboardViewModel(
            UserCount:       userCount,
            ProfileCount:    profiles?.Count,
            PermissionCount: permissionCount,
            CacheHitRate:    cacheStats?.HitRate,
            Activity:        activity,
            JobStats:        jobStats));
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
