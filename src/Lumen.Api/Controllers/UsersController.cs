using Lumen.Modules.Identity.Application.Queries;
using Lumen.Modules.Identity.Application.Users.Delete;
using Lumen.Modules.Identity.Application.Users.Restore;
using Lumen.Modules.Identity.Application.Users.Update;
using Lumen.SharedKernel.Authorization;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/users")]
[PermissionGroup(PermissionGroups.Users)]
public sealed class UsersController : ApiBaseController
{
    public sealed record UpdateUserRequest(string? Email, string? Username);

    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.List)]
    [ProducesResponseType(typeof(ListUsersPagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? state,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListUsersQuery(search, state, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.Get)]
    [ProducesResponseType(typeof(GetUserDetailResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUserDetailQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.Update)]
    [ProducesResponseType(typeof(UpdateUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new UpdateUserCommand(
                UserId: id,
                NewEmail: request.Email,
                NewUsername: request.Username,
                ActorId: GetActorIdentifier()),
            ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new DeleteUserCommand(UserId: id, ActorId: GetActorIdentifier()), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.Restore)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new RestoreUserCommand(UserId: id, ActorId: GetActorIdentifier()), ct);
        return NoContent();
    }
}
