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
    private readonly IUserIdAccessor _userIdAccessor;

    public PermissionAuthorizationHandler(
        IUserPermissionService permissionService,
        IUserIdAccessor userIdAccessor)
    {
        _permissionService = permissionService;
        _userIdAccessor = userIdAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_userIdAccessor.TryGetUserId(context.User, out var userId))
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
