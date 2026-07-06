namespace Lumen.Authorization.Internal;

internal static class AuthorizationCacheKeys
{
    /// <summary>
    /// Cache key for the permission set of a user in a specific scope.
    /// <para>
    /// When <paramref name="scopeId"/> is <c>null</c> the key encodes <c>"global"</c>,
    /// preserving backward-compatible cache isolation from any scoped entry.
    /// </para>
    /// </summary>
    public static string UserPermissions(Guid userId, Guid? scopeId = null)
        => scopeId.HasValue
            ? $"user-permissions:{userId}:{scopeId.Value}"
            : $"user-permissions:{userId}:global";
}
