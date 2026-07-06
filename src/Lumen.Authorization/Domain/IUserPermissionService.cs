namespace Lumen.Authorization.Domain;

public interface IUserPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(
        Guid userId,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default);
}
