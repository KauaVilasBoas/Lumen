namespace AegisIdentity.Domain.Tokens;

/// <summary>
/// Represents a refresh token issued to an authenticated user.
///
/// Design decisions:
/// - <see cref="TokenHash"/> stores a SHA-256 hash of the raw token, never the token itself.
///   The raw token is a 32-byte random value and is only returned to the caller at issuance time.
/// - <see cref="ReplacedByTokenHash"/> enables refresh-token rotation: when a token is rotated,
///   the old token records the hash of its replacement, providing an auditable rotation chain.
/// - <see cref="CreatedByIp"/> is stored for anomaly detection (reuse from unexpected IP).
/// - Revocation is permanent: a revoked token cannot be un-revoked.
/// - The TTL index on <see cref="ExpiresAt"/> in MongoDB ensures automatic cleanup of expired
///   documents. Application code MUST also validate <see cref="IsExpired"/> because TTL
///   cleanup is not instantaneous (~60 s delay).
/// </summary>
public sealed class RefreshToken
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    /// <summary>String representation of the MongoDB ObjectId. Mapped via BsonClassMap.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Id of the <see cref="AegisIdentity.Domain.Users.User"/> this token belongs to.</summary>
    public string UserId { get; init; } = string.Empty;

    // ─── Token data ───────────────────────────────────────────────────────────

    /// <summary>SHA-256 hash of the raw token. Never store the raw token in the database.</summary>
    public string TokenHash { get; init; } = string.Empty;

    /// <summary>IP address of the client that requested this token.</summary>
    public string CreatedByIp { get; init; } = string.Empty;

    // ─── Rotation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// SHA-256 hash of the token that replaced this one during rotation.
    /// Null until this token is rotated.
    /// </summary>
    public string? ReplacedByTokenHash { get; private set; }

    // ─── Lifecycle timestamps ─────────────────────────────────────────────────

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Absolute expiration instant. MongoDB TTL index targets this field.
    /// Application code must also validate this value — TTL cleanup has ~60 s delay.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>Set when the token is revoked. Null for active tokens.</summary>
    public DateTime? RevokedAt { get; private set; }

    // ─── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new, active refresh token.
    /// </summary>
    /// <param name="userId">The owner user's Id.</param>
    /// <param name="tokenHash">SHA-256 hash of the raw token value.</param>
    /// <param name="expiresAt">Absolute expiration instant (UTC).</param>
    /// <param name="createdByIp">IP address of the issuing request.</param>
    public static RefreshToken Create(
        string userId,
        string tokenHash,
        DateTime expiresAt,
        string createdByIp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByIp);

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedByIp = createdByIp,
        };
    }

    // ─── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>Returns true when the token has passed its expiration instant.</summary>
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    /// <summary>Returns true when the token has been explicitly revoked.</summary>
    public bool IsRevoked() => RevokedAt.HasValue;

    /// <summary>
    /// Returns true when the token is active: not expired and not revoked.
    /// Always evaluate this before accepting a refresh-token exchange.
    /// </summary>
    public bool IsActive() => !IsExpired() && !IsRevoked();

    /// <summary>
    /// Revokes this token and records the hash of its replacement (rotation scenario).
    /// </summary>
    /// <param name="replacedByTokenHash">
    /// SHA-256 hash of the new token that replaces this one.
    /// Pass null when revoking without rotation (e.g., explicit logout).
    /// </param>
    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
