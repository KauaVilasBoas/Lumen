using Lumen.SharedKernel.Persistence;

namespace Lumen.Modules.Identity.Domain.Authorization;

internal sealed class PermissionProfile : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid PermissionId { get; init; }

    public Guid ProfileId { get; init; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static PermissionProfile Create(Guid permissionId, Guid profileId)
    {
        if (permissionId == Guid.Empty)
            throw new ArgumentException("PermissionId cannot be empty.", nameof(permissionId));

        if (profileId == Guid.Empty)
            throw new ArgumentException("ProfileId cannot be empty.", nameof(profileId));

        return new PermissionProfile
        {
            PermissionId = permissionId,
            ProfileId = profileId,
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
