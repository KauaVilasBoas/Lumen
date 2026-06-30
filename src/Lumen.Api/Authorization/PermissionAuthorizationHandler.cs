using System.Security.Claims;
using Lumen.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace Lumen.Api.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserPermissionService _permissionService;

    public PermissionAuthorizationHandler(IUserPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
            return;

        var hasPermission = await _permissionService.HasPermissionAsync(userId, requirement.Code);

        if (hasPermission)
            context.Succeed(requirement);
    }
}
