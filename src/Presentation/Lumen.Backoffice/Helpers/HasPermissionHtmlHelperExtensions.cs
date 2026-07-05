using System.Security.Claims;

using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Contracts;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lumen.Backoffice.Helpers;

public static class HasPermissionHtmlHelperExtensions
{
    public static async Task<bool> HasPermissionAsync(
        this IHtmlHelper html,
        string controller,
        string action,
        CancellationToken cancellationToken = default)
    {
        var user = html.ViewContext.HttpContext.User;

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return false;

        var permissionService = html.ViewContext.HttpContext.RequestServices
            .GetRequiredService<IUserPermissionService>();

        var normalizedController = ControllerNameNormalizer.Normalize(controller);
        var permissionCode = $"{normalizedController}.{action}";

        return await permissionService.HasPermissionAsync(userId, permissionCode, cancellationToken);
    }

    public static async Task<IHtmlContent> RenderIfPermittedAsync(
        this IHtmlHelper html,
        string controller,
        string action,
        Func<IHtmlContent> content,
        CancellationToken cancellationToken = default)
    {
        var permitted = await html.HasPermissionAsync(controller, action, cancellationToken);
        return permitted ? content() : HtmlString.Empty;
    }
}
