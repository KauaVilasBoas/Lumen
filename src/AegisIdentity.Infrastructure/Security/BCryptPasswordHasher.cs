using AegisIdentity.Application.Security;

namespace AegisIdentity.Infrastructure.Security;

// Cost factor 12 is the recommended floor for BCrypt in 2024+.
// At cost 12, hashing takes ~250 ms on commodity hardware — fast enough for
// user-facing flows, slow enough to make offline brute-force economically unattractive.
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        return BCrypt.Net.BCrypt.HashPassword(plainText, WorkFactor);
    }

    public bool Verify(string plainText, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return BCrypt.Net.BCrypt.Verify(plainText, hash);
    }
}
