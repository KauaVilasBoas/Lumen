using Lumen.Authorization.Persistence;

namespace Lumen.Authorization.Domain;

public sealed class UserProfile : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid UserId { get; init; }

    public Guid ProfileId { get; init; }

    /// <summary>
    /// The tenant/scope this assignment applies to, or <c>null</c> for a global assignment.
    /// <para>
    /// Global assignments (<c>null</c>) grant permissions regardless of the active scope.
    /// Scoped assignments are evaluated only when the host reports the matching scope as active.
    /// </para>
    /// </summary>
    public Guid? ScopeId { get; init; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Creates a global user-profile assignment (no tenant scope).
    /// </summary>
    public static UserProfile Create(Guid userId, Guid profileId)
        => Create(userId, profileId, scopeId: null);

    /// <summary>
    /// Creates a user-profile assignment, optionally scoped to a tenant.
    /// </summary>
    /// <param name="userId">The user receiving the profile.</param>
    /// <param name="profileId">The profile being assigned.</param>
    /// <param name="scopeId">The tenant scope, or <c>null</c> for a global assignment.</param>
    public static UserProfile Create(Guid userId, Guid profileId, Guid? scopeId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (profileId == Guid.Empty)
            throw new ArgumentException("ProfileId cannot be empty.", nameof(profileId));

        return new UserProfile
        {
            UserId = userId,
            ProfileId = profileId,
            ScopeId = scopeId,
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
