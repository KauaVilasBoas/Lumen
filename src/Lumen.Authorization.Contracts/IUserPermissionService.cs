namespace Lumen.Authorization.Contracts;

public interface IUserPermissionService
{
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
}
