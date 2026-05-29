using AegisIdentity.SharedKernel.Persistence;

namespace AegisIdentity.Domain.Authorization;

public sealed class Permission : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Code { get; private set; } = string.Empty;

    public string Controller { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public Guid? GroupPermissionId { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public static string BuildCode(string controller, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return $"{controller}.{action}";
    }

    public static Permission Create(string controller, string action, string displayName, Guid? groupPermissionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controller);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Permission
        {
            Controller = controller,
            Action = action,
            Code = BuildCode(controller, action),
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
