using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.Application.UserProfiles.Assign;
using Lumen.Authorization.Application.UserProfiles.Remove;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Backoffice.Internal;
using Lumen.Authorization.Backoffice.ViewModels;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;

public sealed class UsersController : LumenBackofficeBaseController
{
    private readonly ISender _sender;
    private readonly IAuthorizationUserSource _userSource;

    public UsersController(ISender sender, IAuthorizationUserSource userSource)
    {
        _sender = sender;
        _userSource = userSource;
    }

    [HttpGet]
    [RequirePermission(LumenBackofficePermissions.UserProfilesManage)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var users = await _userSource.ListActiveUsersAsync(ct);
        var model = await BuildUserListViewModelAsync(users, ct);
        return View(model);
    }

    [HttpGet]
    [RequirePermission(LumenBackofficePermissions.UserProfilesManage)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var users = await _userSource.ListActiveUsersAsync(ct);
        var user = users.FirstOrDefault(u => u.Id == id);

        if (user is null)
            return NotFound();

        TempData.TryGetValue("Error", out var rawError);
        var errorMessage = rawError as string;

        var model = await BuildUserProfileAssignmentViewModelAsync(user, errorMessage, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(LumenBackofficePermissions.UserProfilesManage)]
    public async Task<IActionResult> Assign(Guid userId, Guid profileId, CancellationToken ct)
    {
        try
        {
            await _sender.Send(new AssignUserProfileCommand(userId, profileId), ct);
        }
        catch (AuthorizationNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = BackofficeErrorMessages.AssignProfileError;
        }

        return RedirectToAction(nameof(Details), new { id = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(LumenBackofficePermissions.UserProfilesManage)]
    public async Task<IActionResult> Remove(Guid userId, Guid profileId, CancellationToken ct)
    {
        try
        {
            await _sender.Send(new RemoveUserProfileCommand(userId, profileId), ct);
        }
        catch (AuthorizationNotFoundException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["Error"] = BackofficeErrorMessages.RemoveProfileError;
        }

        return RedirectToAction(nameof(Details), new { id = userId });
    }

    private async Task<UserListViewModel> BuildUserListViewModelAsync(
        IReadOnlyList<AuthorizationUserDto> users,
        CancellationToken ct)
    {
        if (users.Count == 0)
            return new UserListViewModel([], IsEmpty: true);

        var items = new List<UserListItemViewModel>(users.Count);

        foreach (var user in users)
        {
            var assigned = await _sender.Send(new ListUserProfilesQuery(user.Id), ct);
            items.Add(new UserListItemViewModel(user.Id, user.Username, user.Email, user.State, assigned.Count));
        }

        return new UserListViewModel(items, IsEmpty: false);
    }

    private async Task<UserProfileAssignmentViewModel> BuildUserProfileAssignmentViewModelAsync(
        AuthorizationUserDto user,
        string? errorMessage,
        CancellationToken ct)
    {
        var assignedProfiles = await _sender.Send(new ListUserProfilesQuery(user.Id), ct);
        var allProfiles = await _sender.Send(new ListProfilesQuery(), ct);

        return new UserProfileAssignmentViewModel(
            user.Id,
            user.Username,
            user.Email,
            user.State,
            assignedProfiles,
            allProfiles,
            errorMessage);
    }
}
