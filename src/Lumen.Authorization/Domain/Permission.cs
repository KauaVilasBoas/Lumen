using Lumen.Authorization.Persistence;

namespace Lumen.Authorization.Domain;

public sealed class Permission : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public Guid? GroupPermissionId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static Permission Create(string code, string displayName, Guid? groupPermissionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Permission
        {
            Code = code,
            DisplayName = displayName,
            GroupPermissionId = groupPermissionId,
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
