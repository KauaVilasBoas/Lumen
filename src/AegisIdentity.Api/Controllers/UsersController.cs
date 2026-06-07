using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[PermissionGroup("Users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = "Users.List")]
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
