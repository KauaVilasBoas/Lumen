namespace Lumen.Modules.Identity.Domain.Authorization;

internal interface IUserPermissionCache
{
    static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    Task<HashSet<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SetAsync(Guid userId, HashSet<string> codes, CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);
}
