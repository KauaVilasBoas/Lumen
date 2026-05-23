using AegisIdentity.Domain.Users;

namespace AegisIdentity.Application.Security;

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
