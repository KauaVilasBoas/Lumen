using Lumen.SharedKernel.Persistence;

namespace Lumen.Modules.Identity.Domain.Tokens;

internal sealed class PasswordResetToken : IOneTimeToken
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public string TokenHash { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; init; }

    public DateTime? UsedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static PasswordResetToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt)
    {
        OneTimeTokenPolicy.ValidateCreation(tokenHash, expiresAt);

        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
        };
    }

    public bool IsExpired() => OneTimeTokenPolicy.IsExpired(ExpiresAt);

    public bool IsUsed() => OneTimeTokenPolicy.IsUsed(UsedAt);

    public bool IsValid() => OneTimeTokenPolicy.IsValid(ExpiresAt, UsedAt);

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
