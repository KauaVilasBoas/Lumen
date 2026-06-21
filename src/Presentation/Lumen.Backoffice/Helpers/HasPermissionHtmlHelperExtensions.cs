using System.Security.Claims;

using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Authorization;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lumen.Backoffice.Helpers;

/// <summary>
/// Extension methods on <see cref="IHtmlHelper"/> that expose permission checks to Razor views.
///
/// Usage:
/// <code>
/// @if (await Html.HasPermissionAsync("Users", "Delete")) { ... }
/// </code>
///
/// The method normalizes the controller name via <see cref="ControllerNameNormalizer"/>
/// and builds the canonical permission code with <see cref="Permission.BuildCode"/> —
/// the same path followed by AUTH-09/AUTH-11 on the API side.
/// </summary>
public static class HasPermissionHtmlHelperExtensions
{
    /// <summary>
    /// Returns <c>true</c> when the authenticated user has the permission derived from
    /// <paramref name="controller"/> and <paramref name="action"/>; <c>false</c> for
    /// anonymous users or when the permission is absent.
    /// </summary>
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
        var permissionCode = Permission.BuildCode(normalizedController, action);

        return await permissionService.HasPermissionAsync(userId, permissionCode, cancellationToken);
    }

    /// <summary>
    /// Returns <see cref="HtmlString.Empty"/> so callers may use the return value
    /// in Razor expressions, but the primary use case is the boolean overload above.
    /// </summary>
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