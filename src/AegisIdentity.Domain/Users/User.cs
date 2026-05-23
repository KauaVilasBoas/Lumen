using System.Security.Cryptography;

namespace AegisIdentity.Domain.Users;

public sealed class User
{
    public static readonly IReadOnlyList<string> DefaultRoles = ["user"];

    // A valid 24-character lowercase hex string is a valid MongoDB ObjectId representation.
    // Generating it here means the aggregate has identity from birth, not from persistence —
    // which aligns with DDD and makes unit testing straightforward (no MongoDB driver needed).
    public string Id { get; init; } = GenerateObjectId();

    public string Email { get; private set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public List<string> Roles { get; init; } = [..DefaultRoles];

    public bool IsActive { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static User Create(string email, string username, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Email = NormalizeEmail(email),
            Username = username,
            PasswordHash = passwordHash,
        };
    }

    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    public void RecordFailedLogin(int lockoutThreshold, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= lockoutThreshold)
            LockedUntil = DateTime.UtcNow.Add(lockoutDuration);
    }

    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsLockedOut() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    // MongoDB ObjectId layout: 4-byte timestamp + 5-byte random + 3-byte counter.
    // We skip the counter and use 12 random bytes, which is collision-resistant at the
    // scale of this application and produces a valid 24-character lowercase hex Id.
    private static string GenerateObjectId()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
}
