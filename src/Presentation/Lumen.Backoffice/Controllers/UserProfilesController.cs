using Lumen.Backoffice.Services;
using Lumen.Backoffice.ViewModels;
using Lumen.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Backoffice.Controllers;

[Authorize]
public sealed class UserProfilesController : BackofficeBaseController
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

        return View(new UserProfilesViewModel(userId, userProfiles, availableProfiles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid userId, Guid profileId, CancellationToken ct)
    {
        var (success, error) = await _adminApiClient.AssignUserProfileAsync(userId, profileId, ct);

        if (!success)
            TempData["Error"] = error ?? BackofficeErrorMessages.AssignProfileError;

        return RedirectToAction(nameof(Index), new { userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid userId, Guid profileId, CancellationToken ct)
    {
        var (success, error) = await _adminApiClient.RemoveUserProfileAsync(userId, profileId, ct);

        if (!success)
            TempData["Error"] = error ?? BackofficeErrorMessages.RemoveProfileError;

        return RedirectToAction(nameof(Index), new { userId });
    }
}
