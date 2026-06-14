using AegisIdentity.CommandHandlers.Users.Update;
using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Authorization;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

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
    [ProducesResponseType(typeof(ListUsersQueryHandler.PagedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? state,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1)
            return BadRequest(new ValidationProblemDetails
            {
                Errors = { ["page"] = ["Page must be greater than or equal to 1."] }
            });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new ValidationProblemDetails
            {
                Errors = { ["pageSize"] = ["PageSize must be between 1 and 100."] }
            });

        var stateFilter = ParseStateFilter(state);

        if (stateFilter is null)
            return BadRequest(new ValidationProblemDetails
            {
                Errors = { ["state"] = [$"Invalid state value '{state}'. Allowed values: active, locked, pending, deleted, all."] }
            });

        var query = new ListUsersQueryHandler.Query(
            Search: search,
            State: stateFilter.Value,
            Page: page,
            PageSize: pageSize);

        var result = await _mediator.Send(query, ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.Get)]
    [ProducesResponseType(typeof(GetUserDetailQueryHandler.Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUserDetailQueryHandler.Query(id), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Users.Update)]
    [ProducesResponseType(typeof(UpdateUserCommandHandler.Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
    {
        var command = new UpdateUserCommandHandler.Command(
            UserId: id,
            NewEmail: request.Email,
            NewUsername: request.Username,
            ActorId: GetActorIdentifier());

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    private static ListUsersQueryHandler.UserStateFilter? ParseStateFilter(string? state)
        => state?.ToLowerInvariant() switch
        {
            null or "" or "all" => ListUsersQueryHandler.UserStateFilter.All,
            "active"            => ListUsersQueryHandler.UserStateFilter.Active,
            "locked"            => ListUsersQueryHandler.UserStateFilter.Locked,
            "pending"           => ListUsersQueryHandler.UserStateFilter.Pending,
            "deleted"           => ListUsersQueryHandler.UserStateFilter.Deleted,
            _                   => null,
        };
}
