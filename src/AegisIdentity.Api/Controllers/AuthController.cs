using AegisIdentity.CommandHandlers.Auth.Login;
using AegisIdentity.CommandHandlers.Auth.Refresh;
using AegisIdentity.CommandHandlers.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    public sealed record LoginRequest(string Identifier, string Password);

    public sealed record RefreshRequest(string RefreshToken);

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserCommandHandler.Result), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommandHandler.Command command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        return CreatedAtAction(
            actionName: null,
            routeValues: new { id = result.Id },
            value: result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserCommandHandler.Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), 423)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new LoginUserCommandHandler.Command(request.Identifier, request.Password, clientIp);

        var result = await _mediator.Send(command, ct);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenCommandHandler.Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new RefreshTokenCommandHandler.Command(request.RefreshToken, clientIp);

        var result = await _mediator.Send(command, ct);

        return Ok(result);
    }
}
