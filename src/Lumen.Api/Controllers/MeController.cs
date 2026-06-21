using AegisIdentity.CommandHandlers.Users.ChangePassword;
using AegisIdentity.ReadModels.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

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
    [ProducesResponseType(typeof(GetCurrentUserQueryHandler.Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var unauthorized = RequireCurrentUserId(out var userId);
        if (unauthorized is not null) return unauthorized;

        var result = await _mediator.Send(new GetCurrentUserQueryHandler.Query(userId), ct);

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

        var command = new ChangePasswordCommandHandler.Command(userId, request.CurrentPassword, request.NewPassword);
        await _mediator.Send(command, ct);

        return NoContent();
    }
}
