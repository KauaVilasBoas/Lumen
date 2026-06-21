namespace Lumen.Domain.Security;

/// <summary>
/// Port for password hashing and verification.
/// Defined in Domain so that use-case handlers depend on the abstraction,
/// not on any concrete hashing library (BCrypt, Argon2, etc.).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainText);

    bool Verify(string plainText, string hash);
}
