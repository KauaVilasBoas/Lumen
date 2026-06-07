using AegisIdentity.SharedKernel.Authorization;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/authorization-graph")]
[Produces("application/json")]
[PermissionGroup(PermissionGroups.Authorization)]
public sealed class AuthorizationGraphController : ControllerBase
{
    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult View()
    {
        return Ok();
    }
}
