using AegisIdentity.SharedKernel.Persistence;

namespace AegisIdentity.Domain.Users;

public sealed class User : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Email { get; private set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

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

    public void ChangeEmail(string newEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newEmail);

        Email     = NormalizeEmail(newEmail);
        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeUsername(string newUsername)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newUsername);

        Username  = newUsername;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
