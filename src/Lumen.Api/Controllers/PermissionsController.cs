using Lumen.Authorization.Application.Queries;
using Lumen.Authorization.AspNetCore;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/permissions")]
[PermissionGroup(PermissionGroups.Permissions)]
public sealed class PermissionsController : ApiBaseController
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.Permissions.List)]
    [ProducesResponseType(typeof(IReadOnlyList<ListPermissionsGroupResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListPermissionsQuery(), ct);
        return Ok(result);
    }
}
