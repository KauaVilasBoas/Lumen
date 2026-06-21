namespace Lumen.SharedKernel.Constants;

/// <summary>
/// Prefixes and key fragments used when storing or retrieving values from distributed cache.
/// Centralised here so that producers (Infrastructure) and consumers (Application, API) share
/// an identical key format, preventing silent cache misses after a rename.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Prefix for HIBP k-anonymity range responses keyed by their 5-character SHA-1 prefix.
    /// Full key format: <c>"hibp:range:{prefix}"</c> (e.g. <c>"hibp:range:5BAA6"</c>).
    /// </summary>
    public const string HibpRangePrefix = "hibp:range:";

    /// <summary>
    /// Cache key for the set of permission codes granted to a specific user.
    /// Full key format: <c>"user-permissions:{userId}"</c>.
    /// </summary>
    public static string UserPermissions(Guid userId) => $"user-permissions:{userId}";
}
