using Lumen.Authorization.Application.Queries;
using Lumen.SharedKernel.Authorization;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/authorization-graph")]
[PermissionGroup(PermissionGroups.Authorization)]
public sealed class AuthorizationGraphController : ApiBaseController
{
    private readonly IMediator _mediator;

    public AuthorizationGraphController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]
    [ProducesResponseType(typeof(AuthorizationGraphSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> View(CancellationToken ct = default)
    {
        var snapshot = await _mediator.Send(new GetAuthorizationGraphQuery(), ct);
        return Ok(snapshot);
    }
}
