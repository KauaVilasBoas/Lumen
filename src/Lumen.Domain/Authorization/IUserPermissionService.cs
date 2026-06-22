namespace Lumen.Domain.Authorization;

/// <summary>
/// Resolves the permission codes granted to a user, consulting the distributed cache
/// first and falling back to the database on a cache miss.
///
/// This service is the single source of truth for permission lookups — consumed by both
/// the API authorization handler and the Backoffice Razor helpers.  Never bypass it to
/// query the cache or the repository directly.
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Returns the set of permission codes held by <paramref name="userId"/>.
    /// On a cache miss the result is populated from the database and written back to the cache.
    /// </summary>
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="userId"/> holds
    /// <paramref name="permissionCode"/>; <c>false</c> otherwise,
    /// including when the user is not found.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
}
