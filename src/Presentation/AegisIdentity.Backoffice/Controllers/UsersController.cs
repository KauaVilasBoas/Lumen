using AegisIdentity.Backoffice.Services;
using AegisIdentity.Backoffice.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

[Authorize]
public sealed class UsersController : Controller
{
    private readonly AdminApiClient _adminApiClient;

    public UsersController(AdminApiClient adminApiClient)
    {
        _adminApiClient = adminApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? id, CancellationToken ct)
    {
        var page = await _adminApiClient.ListUsersAsync(
            search: null, state: null, page: 1, pageSize: 100, ct);

        var users = page?.Items ?? [];

        if (users.Count == 0)
            return View(new UsersPageViewModel([], null));

        var firstId = id.HasValue && users.Any(u => u.Id == id.Value)
            ? id.Value
            : users[0].Id;

        var selected = await _adminApiClient.GetUserAsync(firstId, ct);

        if (selected is null)
            return View(new UsersPageViewModel([], null));

        var listItems = users.Select(UserViewModelBuilder.ToListItem).ToList();

        return View(new UsersPageViewModel(listItems, UserViewModelBuilder.ToDetail(selected)));
    }
}
