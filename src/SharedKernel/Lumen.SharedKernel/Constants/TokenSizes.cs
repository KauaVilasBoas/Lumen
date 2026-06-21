namespace Lumen.SharedKernel.Constants;

/// <summary>
/// Byte lengths used when generating cryptographically-random tokens.
/// 32 bytes = 256 bits of entropy — sufficient to make brute-force enumeration
/// infeasible for single-use, expiring values stored hashed in the database.
/// </summary>
public static class TokenSizes
{
    /// <summary>
    /// Number of random bytes for the opaque refresh token value emitted by <c>JwtService</c>.
    /// </summary>
    public const int RefreshTokenBytes = 32;

    /// <summary>
    /// Number of random bytes for one-time raw tokens (email confirmation, password reset).
    /// </summary>
    public const int RawTokenBytes = 32;
}
