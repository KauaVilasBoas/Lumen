using AegisIdentity.CommandHandlers.Profiles.CreateProfile;
using AegisIdentity.CommandHandlers.Profiles.DeleteProfile;
using AegisIdentity.CommandHandlers.Profiles.SetProfilePermissions;
using AegisIdentity.CommandHandlers.Profiles.UpdateProfile;
using AegisIdentity.ReadModels.Queries;
using AegisIdentity.SharedKernel.Authorization;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/profiles")]
[Produces("application/json")]
[PermissionGroup(PermissionGroups.Profiles)]
public sealed class ProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.List)]
    [ProducesResponseType(typeof(IReadOnlyList<ListProfilesQueryHandler.Result>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListProfilesQueryHandler.Query(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.Get)]
    [ProducesResponseType(typeof(GetProfileQueryHandler.Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProfileQueryHandler.Query(id), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.Create)]
    [ProducesResponseType(typeof(CreateProfileCommandHandler.Result), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProfileCommandHandler.Command command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var command = new UpdateProfileCommandHandler.Command(id, request.Name, request.Description);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProfileCommandHandler.Command(id), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.SetPermissions)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPermissions(
        Guid id,
        [FromBody] SetPermissionsRequest request,
        CancellationToken ct)
    {
        var actorUsername = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var command = new SetProfilePermissionsCommandHandler.Command(id, request.PermissionIds, actorUsername);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    public sealed record UpdateProfileRequest(string Name, string Description);

    public sealed record SetPermissionsRequest(IReadOnlyList<Guid> PermissionIds);
}
