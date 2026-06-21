using AegisIdentity.SharedKernel.Persistence;

namespace AegisIdentity.Domain.Tokens;

public sealed class RefreshToken : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public string TokenHash { get; init; } = string.Empty;

    public string CreatedByIp { get; init; } = string.Empty;

    public string? ReplacedByTokenHash { get; private set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string createdByIp)
    {
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

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
