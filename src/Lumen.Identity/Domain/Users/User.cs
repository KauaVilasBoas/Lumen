using Lumen.SharedKernel.Persistence;

namespace Lumen.Identity.Domain.Users;

public sealed class User : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Email { get; private set; } = string.Empty;

    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsBootstrap { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? EmailConfirmedAt { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

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

    public static User CreateBootstrap(string email, string username, string passwordHash)
    {
        var user = Create(email, username, passwordHash);
        user.IsBootstrap = true;
        return user;
    }

    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    public void ConfirmEmail()
    {
        IsActive = true;
        EmailConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

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

        Email = NormalizeEmail(newEmail);
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeUsername(string newUsername)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newUsername);

        Username = newUsername;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
