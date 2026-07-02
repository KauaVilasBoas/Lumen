using Lumen.Authorization.AspNetCore;
using Lumen.Modules.Audit.Application;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

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
    [RequirePermission(PermissionCodes.Audit.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<AuditEntryResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Read(
        [FromQuery] int take = ValidationLimits.AuditTakeDefaultValue,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRecentAuditFeedQuery(take), ct);
        return Ok(result);
    }
}
