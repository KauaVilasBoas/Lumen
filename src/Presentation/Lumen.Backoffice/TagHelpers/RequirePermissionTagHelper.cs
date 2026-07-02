using System.Security.Claims;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Contracts;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Lumen.Backoffice.TagHelpers;

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
