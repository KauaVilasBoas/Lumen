using Lumen.Authorization.Application.Profiles.Create;
using Lumen.Authorization.Application.Profiles.Delete;
using Lumen.Authorization.Application.Profiles.SetPermissions;
using Lumen.Authorization.Application.Profiles.Update;
using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Backoffice.Internal;
using Lumen.Authorization.Backoffice.ViewModels;
using Lumen.Authorization.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;

public sealed class ProfilesController : LumenBackofficeBaseController
{
    private readonly ISender _sender;

    public ProfilesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [RequirePermission(LumenBackofficePermissions.ProfilesView)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var profiles = await _sender.Send(new ListProfilesQuery(), ct);
        return View(new ProfileListViewModel(profiles));
    }

    [HttpGet]
    [RequirePermission(LumenBackofficePermissions.ProfilesView)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var profile = await _sender.Send(new GetProfileQuery(id), ct);

        if (profile is null)
            return NotFound();

        var permissions = await _sender.Send(new ListPermissionsQuery(), ct);
        TempData.TryGetValue("Error", out var rawError);
        var errorMessage = rawError as string;

        return View(new ProfileDetailViewModel(profile, permissions, errorMessage));
    }

    [HttpGet]
    [RequirePermission(LumenBackofficePermissions.ProfilesManage)]
    public IActionResult Create() => View(new CreateProfileFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(LumenBackofficePermissions.ProfilesManage)]
    public async Task<IActionResult> Create(CreateProfileFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        try
        {
            await _sender.Send(new CreateProfileCommand(form.Name, form.Description), ct);
            return RedirectToAction(nameof(Index));
        }
        catch (AuthorizationConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, BackofficeErrorMessages.CreateProfileError);
            return View(form);
        }
    }

    [HttpGet]
    [RequirePermission(LumenBackofficePermissions.ProfilesManage)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var profile = await _sender.Send(new GetProfileQuery(id), ct);

        if (profile is null)
            return NotFound();

        if (profile.IsSystem)
            return Forbid();

        return View(new EditProfileFormModel(profile.Id, profile.Name, profile.Description));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(LumenBackofficePermissions.ProfilesManage)]
    public async Task<IActionResult> Edit(EditProfileFormModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(form);

        try
        {
            await _sender.Send(new UpdateProfileCommand(form.Id, form.Name, form.Description), ct);
            return RedirectToAction(nameof(Index));
        }
        catch (AuthorizationConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
        catch (AuthorizationForbiddenException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, BackofficeErrorMessages.UpdateProfileError);
            return View(form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(LumenBackofficePermissions.ProfilesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _sender.Send(new DeleteProfileCommand(id), ct);
        }
        catch (AuthorizationForbiddenException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = BackofficeErrorMessages.DeleteProfileError;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(LumenBackofficePermissions.ProfilesManage)]
    public async Task<IActionResult> SetPermissions(
        Guid id,
        [FromForm] List<Guid> selectedPermissionIds,
        CancellationToken ct)
    {
        try
        {
            await _sender.Send(new SetProfilePermissionsCommand(id, selectedPermissionIds), ct);
        }
        catch (AuthorizationForbiddenException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = BackofficeErrorMessages.SetPermissionsError;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
