using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Authorization;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[Route("api/audit")]
[PermissionGroup(PermissionGroups.Audit)]
public sealed class AuditController : ApiBaseController
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("recent")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Audit.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<GetRecentAuditFeedQueryHandler.AuditEntryResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Read(
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        if (take < 1 || take > 100)
            return BadRequest(new ValidationProblemDetails
            {
                Errors = { ["take"] = ["take must be between 1 and 100."] }
            });

        var query = new GetRecentAuditFeedQueryHandler.Query(take);
        var result = await _mediator.Send(query, ct);

        return Ok(result);
    }
}
