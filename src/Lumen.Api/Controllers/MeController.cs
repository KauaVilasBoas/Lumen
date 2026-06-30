using Lumen.Modules.Identity.Application.Queries;
using Lumen.Modules.Identity.Application.Users.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lumen.Api.Controllers;

[Route("api/me")]
public sealed class MeController : ApiBaseController
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpGet]
    [ProducesResponseType(typeof(GetCurrentUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var unauthorized = RequireCurrentUserId(out var userId);
        if (unauthorized is not null) return unauthorized;

        var result = await _mediator.Send(new GetCurrentUserQuery(userId), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var unauthorized = RequireCurrentUserId(out var userId);
        if (unauthorized is not null) return unauthorized;

        await _mediator.Send(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword), ct);

        return NoContent();
    }
}
