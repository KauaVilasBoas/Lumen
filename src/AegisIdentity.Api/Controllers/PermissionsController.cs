using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Produces("application/json")]
[PermissionGroup("Permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = "Permissions.List")]
    [ProducesResponseType(typeof(IReadOnlyList<ListPermissionsQueryHandler.GroupResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListPermissionsQueryHandler.Query(), ct);
        return Ok(result);
    }
}
