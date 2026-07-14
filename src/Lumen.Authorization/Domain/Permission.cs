using Lumen.Authorization.Persistence;

namespace Lumen.Authorization.Domain;

public sealed class Permission : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Code { get; private set; } = string.Empty;

    public string Controller { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public Guid? GroupPermissionId { get; private set; }

    public bool IsOrphan { get; private set; }

    public DateTime? OrphanedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static string BuildCode(string controller, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return $"{controller}.{action}";
    }

    public static Permission Create(string controller, string action, Guid? groupPermissionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        var code = BuildCode(controller, action);

        return new Permission
        {
            Controller = controller,
            Action = action,
            Code = code,
            DisplayName = code,
            GroupPermissionId = groupPermissionId,
        };
    }

    public void UpdateLocationAndGroup(string controller, string action, Guid? groupPermissionId)
    {
        Controller = controller;
        Action = action;
        GroupPermissionId = groupPermissionId;
    }

    public void MarkAsOrphan()
    {
        IsOrphan = true;
        OrphanedAt = DateTime.UtcNow;
    }

    public void ClearOrphan()
    {
        IsOrphan = false;
        OrphanedAt = null;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
