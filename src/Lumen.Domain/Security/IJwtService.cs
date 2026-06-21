using System.Security.Claims;
using Lumen.Domain.Users;

namespace Lumen.Domain.Security;

/// <summary>
/// Port for JWT generation, refresh-token value creation, and token validation.
/// Defined in Domain so command handlers depend only on the abstraction;
/// the HMAC-SHA-256 implementation lives in Infrastructure.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically secure opaque refresh token value.
    /// </summary>
    string GenerateRefreshTokenValue();

    /// <summary>
    /// Lifetime of the access token in seconds. Matches the value embedded in the JWT "exp" claim.
    /// </summary>
    int AccessTokenExpiresIn { get; }

    /// <summary>
    /// Validates a JWT access token and returns the associated <see cref="ClaimsPrincipal"/> on success.
    /// Returns <c>null</c> if the token is missing, malformed, expired, or has an invalid signature,
    /// issuer, or audience — callers must treat <c>null</c> as an authentication failure.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
