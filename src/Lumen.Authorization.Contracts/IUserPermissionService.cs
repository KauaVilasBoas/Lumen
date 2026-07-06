namespace Lumen.Authorization.Contracts;

public interface IUserPermissionService
{
    /// <summary>
    /// Returns the set of permission codes for <paramref name="userId"/> in the given
    /// <paramref name="scopeId"/>, or global permissions when <paramref name="scopeId"/> is
    /// <c>null</c>.
    /// </summary>
    Task<HashSet<string>> GetPermissionsAsync(
        Guid userId,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if <paramref name="userId"/> holds <paramref name="permissionCode"/>
    /// in the given <paramref name="scopeId"/>, or globally when <paramref name="scopeId"/> is
    /// <c>null</c>.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default);
}
