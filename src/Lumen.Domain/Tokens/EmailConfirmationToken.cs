using AegisIdentity.SharedKernel.Persistence;

namespace AegisIdentity.Domain.Tokens;

public sealed class EmailConfirmationToken : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public string TokenHash { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; init; }

    public DateTime? UsedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static EmailConfirmationToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt)
    {
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

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public bool IsUsed() => UsedAt.HasValue;

    public bool IsValid() => !IsExpired() && !IsUsed();

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
