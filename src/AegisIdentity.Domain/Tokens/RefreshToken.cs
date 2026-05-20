namespace AegisIdentity.Domain.Tokens;

public sealed class RefreshToken
{
    public string Id { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string TokenHash { get; init; } = string.Empty;

    public string CreatedByIp { get; init; } = string.Empty;

    public string? ReplacedByTokenHash { get; private set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; private set; }

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

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked() => RevokedAt.HasValue;

    public bool IsActive() => !IsExpired() && !IsRevoked();

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
