using AegisIdentity.CommandHandlers.Auth.Login;
using AegisIdentity.CommandHandlers.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AegisIdentity.Api.Controllers;

/// <summary>
/// Authentication endpoints: user registration and credential-based login.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    // ── Request models ────────────────────────────────────────────────────────

    /// <summary>
    /// Login request body.
    /// <see cref="Identifier"/> accepts either an email address or a username.
    /// <c>ClientIp</c> is intentionally absent — it is extracted from the HTTP connection
    /// by the controller and injected into the command, not sourced from the client.
    /// </summary>
    public sealed record LoginRequest(string Identifier, string Password);

    // ── Actions ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <remarks>
    /// Creates an inactive user account and sends a confirmation email.
    /// The account becomes active only after the email link is confirmed.
    /// The email-send step is fail-open: SMTP failures are logged but do not abort
    /// the registration — 201 is still returned.
    /// </remarks>
    /// <response code="201">Account created. Returns the new user's id, email and username.</response>
    /// <response code="400">Validation failure (empty fields, invalid email format, etc.).</response>
    /// <response code="409">Email or username already in use.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserCommandHandler.Result.Success), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommandHandler.Command command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        return result switch
        {
            RegisterUserCommandHandler.Result.Success success =>
                CreatedAtAction(
                    actionName: null,
                    routeValues: new { id = success.Id },
                    value: success),

            RegisterUserCommandHandler.Result.WeakPassword weak =>
                ValidationProblem(
                    ModelStateDictionaryFromErrors("password", weak.Errors)),

            RegisterUserCommandHandler.Result.DuplicateEmail =>
                Conflict(new { error = "Este email já está em uso." }),

            RegisterUserCommandHandler.Result.DuplicateUsername =>
                Conflict(new { error = "Este username já está em uso." }),

            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    /// <summary>
    /// Authenticate a user and obtain access/refresh tokens.
    /// </summary>
    /// <remarks>
    /// Accepts an email address or username as the identifier.
    /// Returns a JWT access token (short-lived) and an opaque refresh token (long-lived).
    /// Accounts locked due to repeated failed attempts receive 423 Locked.
    /// </remarks>
    /// <response code="200">Authentication successful. Returns access token, refresh token and expiry.</response>
    /// <response code="400">Validation failure (empty identifier or password).</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="403">Email address not yet confirmed.</response>
    /// <response code="423">Account temporarily locked due to repeated failed attempts.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserCommandHandler.Result.Success), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(423)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        // ClientIp is not sourced from the JSON body — it is extracted from the HTTP
        // connection at the presentation boundary and composed into the command here.
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new LoginUserCommandHandler.Command(request.Identifier, request.Password, clientIp);

        var result = await _mediator.Send(command, ct);

        return result switch
        {
            LoginUserCommandHandler.Result.Success success => Ok(success),

            LoginUserCommandHandler.Result.InvalidCredentials => Unauthorized(),

            LoginUserCommandHandler.Result.EmailNotConfirmed => Forbid(),

            LoginUserCommandHandler.Result.AccountLocked => StatusCode(423),

            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ModelStateDictionary ModelStateDictionaryFromErrors(
        string field,
        IEnumerable<string> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in errors)
            modelState.AddModelError(field, error);
        return modelState;
    }
}
