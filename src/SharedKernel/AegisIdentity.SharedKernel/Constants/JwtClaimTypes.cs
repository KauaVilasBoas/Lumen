namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Claim type names used when building and reading JWT tokens.
/// These must match between the token-issuing code (Infrastructure) and any
/// consumer that reads the claims (e.g. authorization policies).
/// </summary>
public static class JwtClaimTypes
{
    /// <summary>
    /// Carries the authenticated user's display username.
    /// Registered alongside the standard <c>sub</c>, <c>email</c> and <c>jti</c> claims.
    /// </summary>
    public const string Username = "username";
}
