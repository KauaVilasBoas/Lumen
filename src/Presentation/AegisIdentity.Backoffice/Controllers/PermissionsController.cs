using AegisIdentity.Backoffice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

/// <summary>
/// Read-only catalogue of discovered permissions (Controller.Action), grouped,
/// with orphan flags. Backed by <c>GET /api/permissions</c>.
/// </summary>
[Authorize]
public sealed class PermissionsController : Controller
{
    private readonly AdminApiClient _adminApiClient;

    public PermissionsController(AdminApiClient adminApiClient) => _adminApiClient = adminApiClient;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var groups = await _adminApiClient.ListPermissionsAsync(ct) ?? [];
        return View(groups);
    }
}
