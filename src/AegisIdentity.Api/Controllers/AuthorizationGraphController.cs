using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Authorization;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/authorization-graph")]
[Produces("application/json")]
[PermissionGroup(PermissionGroups.Authorization)]
public sealed class AuthorizationGraphController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationGraphController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.AuthorizationGraph.View)]
    [ProducesResponseType(typeof(GetAuthorizationGraphQueryHandler.GraphSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        var snapshot = await _mediator.Send(new GetAuthorizationGraphQueryHandler.Query(), ct);
        return Ok(snapshot);
    }
}
