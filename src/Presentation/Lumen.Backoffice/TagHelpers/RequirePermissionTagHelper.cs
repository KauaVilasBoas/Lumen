using System.Security.Claims;
using Lumen.Modules.Identity.Contracts;
using Lumen.SharedKernel.Authorization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Lumen.Backoffice.TagHelpers;

/// <summary>
/// Suppresses an HTML element when the current user lacks the specified permission.
///
/// Usage:
/// <code>
/// &lt;div asp-require-permission-controller="Users" asp-require-permission-action="Delete"&gt;
///     Delete button here
/// &lt;/div&gt;
/// </code>
///
/// Anonymous users always see the element suppressed.  The permission code is built
/// via <see cref="ControllerNameNormalizer"/> + <see cref="Permission.BuildCode"/>,
/// matching the same normalization used by AUTH-09/AUTH-11 on the API side.
/// </summary>
[HtmlTargetElement(Attributes = PermissionControllerAttribute)]
[HtmlTargetElement(Attributes = PermissionActionAttribute)]
public sealed class RequirePermissionTagHelper : TagHelper
{
    private const string PermissionControllerAttribute = "asp-require-permission-controller";
    private const string PermissionActionAttribute = "asp-require-permission-action";

    private readonly IUserPermissionService _permissionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequirePermissionTagHelper(
        IUserPermissionService permissionService,
        IHttpContextAccessor httpContextAccessor)
    {
        _permissionService = permissionService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HtmlAttributeName(PermissionControllerAttribute)]
    public string? Controller { get; set; }

    [HtmlAttributeName(PermissionActionAttribute)]
    public string? Action { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var sub = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
        {
            output.SuppressOutput();
            return;
        }

        if (string.IsNullOrWhiteSpace(Controller) || string.IsNullOrWhiteSpace(Action))
        {
            output.SuppressOutput();
            return;
        }

        var normalizedController = ControllerNameNormalizer.Normalize(Controller);
        var permissionCode = $"{normalizedController}.{Action}";

        var hasPermission = await _permissionService.HasPermissionAsync(userId, permissionCode);

        if (!hasPermission)
            output.SuppressOutput();

        output.Attributes.RemoveAll(PermissionControllerAttribute);
        output.Attributes.RemoveAll(PermissionActionAttribute);
    }
}
