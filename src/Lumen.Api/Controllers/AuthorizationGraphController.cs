using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.AspNetCore;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/authorization-graph")]
public sealed class AuthorizationGraphController : ApiBaseController
{
    private readonly IMediator _mediator;

    public AuthorizationGraphController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AuthorizationGraph.View)]
    [ProducesResponseType(typeof(AuthorizationGraphSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> View(CancellationToken ct = default)
    {
        var snapshot = await _mediator.Send(new GetAuthorizationGraphQuery(), ct);
        return Ok(snapshot);
    }
}
