namespace AegisIdentity.Domain.Tokens;

/// <summary>
/// A single-use token for confirming a new user's email address.
///
/// Design decisions:
/// - Single-use enforced by <see cref="UsedAt"/>: once set, <see cref="IsValid"/> returns false.
/// - <see cref="TokenHash"/> stores a SHA-256 hash of the raw token. Raw token is never persisted.
/// - The TTL index on <see cref="ExpiresAt"/> in MongoDB removes expired documents automatically.
///   Application code MUST also call <see cref="IsExpired"/> because TTL cleanup has ~60 s delay.
/// </summary>
public sealed class EmailConfirmationToken
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    /// <summary>String representation of the MongoDB ObjectId. Mapped via BsonClassMap.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Id of the <see cref="AegisIdentity.Domain.Users.User"/> this token belongs to.</summary>
    public string UserId { get; init; } = string.Empty;

    // ─── Token data ───────────────────────────────────────────────────────────

    /// <summary>SHA-256 hash of the raw token. Never store the raw token in the database.</summary>
    public string TokenHash { get; init; } = string.Empty;

    // ─── Lifecycle timestamps ─────────────────────────────────────────────────

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Absolute expiration instant. MongoDB TTL index targets this field.
    /// Application code must also validate this value — TTL cleanup has ~60 s delay.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>Set when the token is consumed to confirm the email. Null for unused tokens.</summary>
    public DateTime? UsedAt { get; private set; }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new, unused email confirmation token.
    /// </summary>
    /// <param name="userId">The owner user's Id.</param>
    /// <param name="tokenHash">SHA-256 hash of the raw token value.</param>
    /// <param name="expiresAt">Absolute expiration instant (UTC).</param>
    public static EmailConfirmationToken Create(
        string userId,
        string tokenHash,
        DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));

        return new EmailConfirmationToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
    }

    // ─── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>Returns true when the token has passed its expiration instant.</summary>
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    /// <summary>Returns true when the token has already been used.</summary>
    public bool IsUsed() => UsedAt.HasValue;

    /// <summary>
    /// Returns true when the token is valid for use: not expired and not yet used.
    /// Always evaluate this before accepting an email confirmation request.
    /// </summary>
    public bool IsValid() => !IsExpired() && !IsUsed();

    /// <summary>
    /// Marks the token as consumed. Once called, <see cref="IsValid"/> will return false.
    /// </summary>
    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}
