namespace Lumen.Authorization.Domain;

/// <summary>
/// Per-request permission cache keyed by <c>(userId, scopeId)</c>.
/// <para>
/// A <c>null</c> <paramref name="scopeId"/> represents the global permission set
/// (no active tenant). This preserves backward compatibility for non-tenant hosts.
/// </para>
/// </summary>
public interface IUserPermissionCache
{
    static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    Task<HashSet<string>?> GetAsync(Guid userId, Guid? scopeId = null, CancellationToken cancellationToken = default);

    Task SetAsync(Guid userId, Guid? scopeId, HashSet<string> codes, CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid userId, Guid? scopeId = null, CancellationToken cancellationToken = default);
}
