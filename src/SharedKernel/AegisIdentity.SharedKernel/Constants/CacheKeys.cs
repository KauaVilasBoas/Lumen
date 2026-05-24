namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Prefixes and key fragments used when storing or retrieving values from IMemoryCache.
/// Centralised here so that producers (Infrastructure) and any future consumers share
/// an identical key format, preventing silent cache misses after a rename.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Prefix for HIBP k-anonymity range responses keyed by their 5-character SHA-1 prefix.
    /// Full key format: <c>"hibp:range:{prefix}"</c> (e.g. <c>"hibp:range:5BAA6"</c>).
    /// </summary>
    public const string HibpRangePrefix = "hibp:range:";
}
