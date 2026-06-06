using AegisIdentity.Backoffice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

/// <summary>
/// Landing pages for authenticated Backoffice users: the Overview dashboard
/// and the static Architecture explainer.
/// </summary>
[Authorize]
public sealed class HomeController : Controller
{
    private readonly AdminApiClient _adminApiClient;

    public HomeController(AdminApiClient adminApiClient) => _adminApiClient = adminApiClient;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Best-effort live counts for the KPI cards. Any failure falls back to
        // the view's built-in defaults — the dashboard always renders.
        try
        {
            var profiles = await _adminApiClient.ListProfilesAsync(ct);
            if (profiles is not null) ViewBag.ProfileCount = profiles.Count;

            var permissionGroups = await _adminApiClient.ListPermissionsAsync(ct);
            if (permissionGroups is not null)
            {
                var all = permissionGroups.SelectMany(g => g.Permissions).ToList();
                ViewBag.PermissionCount = all.Count;
                ViewBag.OrphanCount = all.Count(p => p.IsOrphan);
            }
        }
        catch
        {
            // swallow — KPI cards degrade gracefully to static defaults
        }

        return View();
    }

    /// <summary>The "how it works" architecture explainer (fully static).</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Architecture() => View();
}
