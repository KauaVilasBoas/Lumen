using AegisIdentity.Domain.Users;

namespace AegisIdentity.Domain.Security;

/// <summary>
/// Port for JWT generation and refresh-token value creation.
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
}
