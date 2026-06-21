using System.Text.Json;
using AegisIdentity.Backoffice.Services;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

[Authorize]
public sealed class AuthorizationGraphController : BackofficeBaseController
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IUserPermissionService _permissionService;
    private readonly AdminApiClient _adminApiClient;

    public AuthorizationGraphController(
        IUserPermissionService permissionService,
        AdminApiClient adminApiClient)
    {
        _permissionService = permissionService;
        _adminApiClient = adminApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        if (!await CallerHasPermissionAsync())
            return Forbid();

        var snapshot = await _adminApiClient.GetAuthorizationGraphAsync(ct);

        ViewBag.GraphJson = snapshot is not null
            ? JsonSerializer.Serialize(snapshot, JsonOpts)
            : null;

        return View();
    }

    private async Task<bool> CallerHasPermissionAsync()
    {
        if (!TryGetCurrentUserId(out var userId))
            return false;

        return await _permissionService.HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View);
    }
}
