using Lumen.Backoffice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Backoffice.Controllers;

[Authorize]
public sealed class PermissionsController : BackofficeBaseController
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
