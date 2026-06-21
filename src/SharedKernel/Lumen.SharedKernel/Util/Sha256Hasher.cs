using System.Security.Cryptography;
using System.Text;

namespace Lumen.SharedKernel.Util;

/// <summary>
/// Deterministic SHA-256 hashing helpers used throughout the application to
/// hash sensitive one-time tokens before persisting them in the database.
/// Storing only the hash (not the raw value) ensures a database leak does not
/// expose usable tokens.
/// </summary>
public static class Sha256Hasher
{
    /// <summary>
    /// Computes the SHA-256 hash of <paramref name="input"/> (UTF-8 encoded) and
    /// returns the result as a lowercase hex string.
    /// </summary>
    /// <param name="input">The plain-text value to hash. Must not be <see langword="null"/>.</param>
    /// <returns>A 64-character lowercase hex string representing the SHA-256 digest.</returns>
    public static string ComputeHex(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
