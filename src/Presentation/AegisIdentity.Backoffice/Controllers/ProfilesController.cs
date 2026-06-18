using AegisIdentity.Backoffice.Services;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

[Authorize]
public sealed class ProfilesController : BackofficeBaseController
{
    private readonly AdminApiClient _adminApiClient;

    public ProfilesController(AdminApiClient adminApiClient)
    {
        _adminApiClient = adminApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profiles = await _adminApiClient.ListProfilesAsync(ct) ?? [];
        return View(profiles);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var profile = await _adminApiClient.GetProfileAsync(id, ct);

        if (profile is null)
            return NotFound();

        var permissions = await _adminApiClient.ListPermissionsAsync(ct) ?? [];

        ViewData["AllPermissions"] = permissions;
        return View(profile);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateProfileFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProfileFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        var (success, error) = await _adminApiClient.CreateProfileAsync(form.Name, form.Description, ct);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? BackofficeErrorMessages.CreateProfileError);
            return View(form);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var profile = await _adminApiClient.GetProfileAsync(id, ct);

        if (profile is null)
            return NotFound();

        if (profile.IsSystem)
            return Forbid();

        return View(new EditProfileFormModel(profile.Id, profile.Name, profile.Description));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        var (success, error) = await _adminApiClient.UpdateProfileAsync(
            form.Id, form.Name, form.Description, ct);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? BackofficeErrorMessages.UpdateProfileError);
            return View(form);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var (success, error) = await _adminApiClient.DeleteProfileAsync(id, ct);

        if (!success)
            TempData["Error"] = error ?? BackofficeErrorMessages.DeleteProfileError;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPermissions(
        Guid id,
        [FromForm] List<Guid> selectedPermissionIds,
        CancellationToken ct)
    {
        var (success, error) = await _adminApiClient.SetProfilePermissionsAsync(id, selectedPermissionIds, ct);

        if (!success)
            TempData["Error"] = error ?? BackofficeErrorMessages.SetPermissionsError;

        return RedirectToAction(nameof(Details), new { id });
    }

    public sealed record CreateProfileFormModel(string Name = "", string Description = "");

    public sealed record EditProfileFormModel(Guid Id, string Name, string Description);
}
