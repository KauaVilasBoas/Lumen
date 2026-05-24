using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Backoffice.Controllers;

/// <summary>
/// Landing page for authenticated Backoffice users.
/// All actions require a valid session cookie — unauthenticated requests are
/// redirected to <c>/Account/Login</c> by the cookie auth middleware.
/// </summary>
[Authorize]
public sealed class HomeController : Controller
{
    /// <summary>
    /// Renders the authenticated home page.
    /// The view invokes the <c>UserDetail</c> ViewComponent which reads
    /// user claims from <see cref="Microsoft.AspNetCore.Mvc.ViewFeatures.IViewContextAware"/>.
    /// </summary>
    [HttpGet]
    public IActionResult Index() => View();
}
