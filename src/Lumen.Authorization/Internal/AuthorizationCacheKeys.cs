namespace Lumen.Authorization.Internal;

internal static class AuthorizationCacheKeys
{
    public static string UserPermissions(Guid userId) => $"user-permissions:{userId}";
}
