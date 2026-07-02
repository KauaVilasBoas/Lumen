using Lumen.Authorization.Application.Profiles.Create;
using Lumen.Authorization.Application.Profiles.Delete;
using Lumen.Authorization.Application.Profiles.SetPermissions;
using Lumen.Authorization.Application.Profiles.Update;
using Lumen.Authorization.Application.Queries;
using Lumen.SharedKernel.Authorization;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/profiles")]
[PermissionGroup(PermissionGroups.Profiles)]
public sealed class ProfilesController : ApiBaseController
{
    private readonly IMediator _mediator;

    public ProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.List)]
    [ProducesResponseType(typeof(IReadOnlyList<ListProfilesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListProfilesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.Get)]
    [ProducesResponseType(typeof(GetProfileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProfileQuery(id), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission]
    [Authorize(Policy = PermissionCodes.Profiles.Create)]
    [ProducesResponseType(typeof(CreateProfileResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProfileCommand command,
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
        await _mediator.Send(new UpdateProfileCommand(id, request.Name, request.Description), ct);
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
        await _mediator.Send(new DeleteProfileCommand(id), ct);
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
        await _mediator.Send(new SetProfilePermissionsCommand(id, request.PermissionIds, GetActorIdentifier()), ct);
        return NoContent();
    }

    public sealed record UpdateProfileRequest(string Name, string Description);

    public sealed record SetPermissionsRequest(IReadOnlyList<Guid> PermissionIds);
}
