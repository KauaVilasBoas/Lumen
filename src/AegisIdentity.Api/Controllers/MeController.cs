using System.Security.Claims;
using AegisIdentity.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
[Produces("application/json")]
public sealed class MeController : ControllerBase
{
    /// <summary>
    /// Returns the identity claims of the currently authenticated user.
    /// Requires a valid Bearer token; returns 401 when the token is absent or invalid.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
    {
        var principal = User;

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var username = principal.FindFirstValue(JwtClaimTypes.Username) ?? string.Empty;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        return Ok(new MeResponse(sub, email, username, roles));
    }

    /// <summary>Projection of the authenticated user's identity claims.</summary>
    public sealed record MeResponse(
        string Sub,
        string Email,
        string Username,
        string[] Roles);
}
