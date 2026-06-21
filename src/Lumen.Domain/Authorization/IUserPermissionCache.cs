namespace Lumen.Domain.Authorization;

/// <summary>
/// Read/write cache for the permission codes granted to a user.
///
/// Resiliency contract: Redis unavailability must NOT propagate as an exception that
/// halts authorization.  Callers should treat a null result from <see cref="GetAsync"/>
/// as a cache miss and fall back to the database.  The fallback implementation lives in
/// AUTH-11; this interface defines only the cache boundary.
///
/// Invalidation is event-driven (AUTH-11/AUTH-14): callers invoke
/// <see cref="InvalidateAsync"/> whenever a user's permission set changes.
/// A short TTL (<see cref="DefaultTtl"/>) acts as a safety net only.
/// </summary>
public interface IUserPermissionCache
{
    /// <summary>Safety-net TTL applied to every cached entry.</summary>
    static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns the cached permission codes for <paramref name="userId"/>,
    /// or <c>null</c> when the entry is absent or Redis is unreachable.
    /// </summary>
    Task<HashSet<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="codes"/> for <paramref name="userId"/> with the default TTL.</summary>
    Task SetAsync(Guid userId, HashSet<string> codes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the cached entry for <paramref name="userId"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="GetAsync"/> and <see cref="SetAsync"/>, this operation is
    /// <b>fail-closed</b>: if the underlying cache store is unavailable, the exception is
    /// propagated to the caller rather than swallowed.  Silently ignoring an invalidation
    /// failure would leave a revoked permission alive in cache until TTL expiry — a
    /// security regression (fail-open on revocation).
    /// </remarks>
    /// <exception cref="Exception">
    /// Re-throws whatever the cache store raises when the remove operation fails.
    /// </exception>
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);
}
