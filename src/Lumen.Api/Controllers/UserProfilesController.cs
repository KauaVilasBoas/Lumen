using Lumen.Modules.Identity.Application.Queries;
using Lumen.Modules.Identity.Application.UserProfiles.Assign;
using Lumen.Modules.Identity.Application.UserProfiles.Remove;
using Lumen.SharedKernel.Authorization;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/users/{userId:guid}/profiles")]
[PermissionGroup(PermissionGroups.UserProfiles)]
public sealed class UserProfilesController : ApiBaseController
{
    private readonly IMediator _mediator;

    public UserProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.UserProfiles.List)]
    [ProducesResponseType(typeof(IReadOnlyList<ListUserProfilesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListUserProfilesQuery(userId), ct);
        return Ok(result);
    }

    [HttpPost("{profileId:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.UserProfiles.Assign)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(Guid userId, Guid profileId, CancellationToken ct)
    {
        await _mediator.Send(new AssignUserProfileCommand(userId, profileId), ct);
        return NoContent();
    }

    [HttpDelete("{profileId:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.UserProfiles.Remove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid userId, Guid profileId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveUserProfileCommand(userId, profileId), ct);
        return NoContent();
    }
}
