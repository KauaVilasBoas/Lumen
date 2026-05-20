namespace AegisIdentity.Domain.Tokens;

public sealed class PasswordResetToken
{
    public string Id { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string TokenHash { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; init; }

    public DateTime? UsedAt { get; private set; }

    public static PasswordResetToken Create(
        string userId,
        string tokenHash,
        DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));

        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public bool IsUsed() => UsedAt.HasValue;

    public bool IsValid() => !IsExpired() && !IsUsed();

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}
