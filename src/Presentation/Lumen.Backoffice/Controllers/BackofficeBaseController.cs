using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Backoffice.Controllers;

public abstract class BackofficeBaseController : Controller
{
    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out userId);
    }
}
