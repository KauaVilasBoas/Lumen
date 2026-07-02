using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Backoffice.Internal;
using Lumen.Authorization.Backoffice.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Authorization.Backoffice.Areas.Lumen.Controllers;

[PermissionGroup(BackofficeRouteDefaults.PermissionsController)]
[RequirePermission]
public sealed class PermissionsController : LumenBackofficeBaseController
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [RequirePermission]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var groups = await _sender.Send(new ListPermissionsQuery(), ct);
        return View(new PermissionCatalogueViewModel(groups));
    }
}
