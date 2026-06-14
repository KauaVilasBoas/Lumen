using System.Security.Claims;
using AegisIdentity.CommandHandlers.Auth.ConfirmEmail;
using AegisIdentity.CommandHandlers.Auth.ForgotPassword;
using AegisIdentity.CommandHandlers.Auth.Login;
using AegisIdentity.CommandHandlers.Auth.Logout;
using AegisIdentity.CommandHandlers.Auth.Refresh;
using AegisIdentity.CommandHandlers.Auth.Register;
using AegisIdentity.CommandHandlers.Auth.ResendConfirmationEmail;
using AegisIdentity.CommandHandlers.Auth.ResetPassword;
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

    public sealed record ForgotPasswordRequest(string Email);

    public sealed record ResendConfirmationRequest(string Email);

    public sealed record ResetPasswordRequest(string Token, string NewPassword);

    public sealed record LoginRequest(string Identifier, string Password);

    public sealed record RefreshRequest(string RefreshToken);

    public sealed record LogoutRequest(string? RefreshToken);

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string token,
        CancellationToken ct)
    {
        var command = new ConfirmEmailCommandHandler.Command(token);
        await _mediator.Send(command, ct);
        return Ok();
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] ResendConfirmationRequest request,
        CancellationToken ct)
    {
        var command = new ResendConfirmationEmailCommandHandler.Command(request.Email);
        await _mediator.Send(command, ct);
        return Ok();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken ct)
    {
        var command = new ResetPasswordCommandHandler.Command(request.Token, request.NewPassword);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken ct)
    {
        var command = new ForgotPasswordCommandHandler.Command(request.Email);
        await _mediator.Send(command, ct);
        return Ok();
    }

    [HttpPost("register")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new LogoutUserCommandHandler.Command(request.RefreshToken, userId, clientIp);

        await _mediator.Send(command, ct);

        return NoContent();
    }
}
