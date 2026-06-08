using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AegisIdentity.Api.Hubs;

[Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]
public sealed class AuthorizationGraphHub : Hub<IAuthorizationGraphHubClient>
{
    private readonly IUserPermissionService _permissionService;

    public AuthorizationGraphHub(IUserPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public override async Task OnConnectedAsync()
    {
        if (!await CallerHasPermissionAsync())
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    private async Task<bool> CallerHasPermissionAsync()
    {
        var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
            return false;

        return await _permissionService.HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View);
    }
}
