using AegisIdentity.Backoffice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

[Authorize]
public sealed class UserProfilesController : Controller
{
    private readonly AdminApiClient _adminApiClient;

    public UserProfilesController(AdminApiClient adminApiClient)
    {
        _adminApiClient = adminApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid userId, CancellationToken ct)
    {
        var userProfiles = await _adminApiClient.ListUserProfilesAsync(userId, ct) ?? [];
        var allProfiles = await _adminApiClient.ListProfilesAsync(ct) ?? [];

        var assignedProfileIds = new HashSet<Guid>(userProfiles.Select(up => up.ProfileId));
        var availableProfiles = allProfiles.Where(p => !assignedProfileIds.Contains(p.Id)).ToList();

        ViewData["UserId"] = userId;
        ViewData["AvailableProfiles"] = availableProfiles;

        return View(userProfiles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid userId, Guid profileId, CancellationToken ct)
    {
        var (success, error) = await _adminApiClient.AssignUserProfileAsync(userId, profileId, ct);

        if (!success)
            TempData["Error"] = error ?? "Erro ao atribuir perfil.";

        return RedirectToAction(nameof(Index), new { userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid userId, Guid profileId, CancellationToken ct)
    {
        var (success, error) = await _adminApiClient.RemoveUserProfileAsync(userId, profileId, ct);

        if (!success)
            TempData["Error"] = error ?? "Erro ao remover perfil.";

        return RedirectToAction(nameof(Index), new { userId });
    }
}
