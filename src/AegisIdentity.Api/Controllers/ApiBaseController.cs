using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiBaseController : ControllerBase
{
    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }

    protected IActionResult? RequireCurrentUserId(out Guid userId)
    {
        if (TryGetCurrentUserId(out userId))
            return null;

        return Unauthorized();
    }

    protected string GetClientIpAddress()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    protected string GetActorIdentifier()
        => User.Identity?.Name
           ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? string.Empty;
}
