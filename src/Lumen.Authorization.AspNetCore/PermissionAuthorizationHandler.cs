using System.Security.Claims;
using Lumen.Authorization.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace Lumen.Authorization.AspNetCore;

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

        var code = ResolvePermissionCode(context, requirement);

        if (code is null)
            return;

        var hasPermission = await _permissionService.HasPermissionAsync(userId, code);

        if (hasPermission)
            context.Succeed(requirement);
    }

    private static string? ResolvePermissionCode(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (requirement.Code is not null)
            return requirement.Code;

        if (context.Resource is not HttpContext httpContext)
            return null;

        var endpoint = httpContext.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (actionDescriptor is null)
            return null;

        var normalizedController = ControllerNameNormalizer.Normalize(
            actionDescriptor.ControllerTypeInfo.Name);

        return $"{normalizedController}.{actionDescriptor.MethodInfo.Name}";
    }
}
