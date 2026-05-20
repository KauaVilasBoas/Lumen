namespace AegisIdentity.Domain.Users;

/// <summary>
/// Aggregate root representing an identity user.
///
/// Design decisions:
/// - <see cref="Id"/> is a string representation of MongoDB's ObjectId. The BSON class map
///   in Infrastructure maps it to ObjectId on the wire, keeping this aggregate free of any
///   persistence dependency.
/// - <see cref="Email"/> is always stored in its normalised form (lowercase + trimmed).
///   Callers MUST use <see cref="NormalizeEmail"/> before comparing or persisting.
/// - <see cref="Roles"/> defaults to ["user"]. Role elevation happens via a dedicated
///   use-case, never by direct property assignment from outside the aggregate.
/// - Account locking state (<see cref="FailedLoginAttempts"/>, <see cref="LockedUntil"/>)
///   is mutated via <see cref="RecordFailedLogin"/> and <see cref="Unlock"/> to keep
///   invariant enforcement inside the aggregate.
/// - No ASP.NET Core Identity dependency — model is intentionally lean for the MVP scope.
/// </summary>
public sealed class User
{
    public static readonly IReadOnlyList<string> DefaultRoles = ["user"];

    // ─── Identity ─────────────────────────────────────────────────────────────

    /// <summary>
    /// String representation of the MongoDB ObjectId.
    /// Mapped to BSON ObjectId (_id) via BsonClassMap in Infrastructure.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Normalised email (lowercase + trimmed). Set via factory only.</summary>
    public string Email { get; private set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // ─── Authorisation ────────────────────────────────────────────────────────

    /// <summary>Simple role list. Defaults to ["user"]. Do not assign directly.</summary>
    public List<string> Roles { get; init; } = [..DefaultRoles];

    // ─── Account state ────────────────────────────────────────────────────────

    /// <summary>False until the user confirms their email address.</summary>
    public bool IsActive { get; set; }

    public DateTime? EmailConfirmedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // ─── Brute-force protection ───────────────────────────────────────────────

    public int FailedLoginAttempts { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    // ─── Audit ────────────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new, inactive user with a normalised email.
    /// The caller is responsible for hashing the password before passing it here.
    /// </summary>
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

    // ─── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>
    /// Normalises an email address to lowercase + trimmed form.
    /// Must be applied before every comparison or persistence call.
    /// </summary>
    public static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Records a failed login attempt and locks the account after the threshold.
    /// </summary>
    /// <param name="lockoutThreshold">Number of failures before a lockout.</param>
    /// <param name="lockoutDuration">How long the account should be locked.</param>
    public void RecordFailedLogin(int lockoutThreshold, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= lockoutThreshold)
            LockedUntil = DateTime.UtcNow.Add(lockoutDuration);
    }

    /// <summary>Resets the failed-login counter and removes the lock.</summary>
    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Returns true when the account is currently within a lockout window.</summary>
    public bool IsLockedOut() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
}
