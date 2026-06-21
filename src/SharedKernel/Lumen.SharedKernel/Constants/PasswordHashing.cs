namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Constants related to password hashing behaviour shared across the application.
/// </summary>
public static class PasswordHashing
{
    /// <summary>
    /// Pre-computed BCrypt hash (cost factor 12) used exclusively for constant-time
    /// verification when the requested user does not exist.
    /// Without this call, an unknown-user path would return in ~1 ms while a
    /// wrong-password path costs ~250 ms (BCrypt work factor 12), leaking which
    /// identifiers are registered via timing differences.
    /// </summary>
    public const string DummyBcryptHash =
        "$2a$12$eImiTXuWVxfM37uY4JANjQu8bkE5KNn3M6GZjTZJfqMV/kI0KZjUe";
}
