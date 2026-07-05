using Lumen.Authorization.Persistence;

namespace Lumen.Authorization.Domain;

public sealed class UserProfile : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public Guid ProfileId { get; init; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static UserProfile Create(Guid userId, Guid profileId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (profileId == Guid.Empty)
            throw new ArgumentException("ProfileId cannot be empty.", nameof(profileId));

        return new UserProfile
        {
            UserId = userId,
            ProfileId = profileId,
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
