namespace Lumen.Modules.Identity.Domain.Authorization;

internal interface IPermissionRepository
{
    Task<Permission?> FindByIdAsync(Guid id, CancellationToken ct = default);

    Task<Permission?> FindByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken ct = default);

    Task InsertAsync(Permission permission, CancellationToken ct = default);

    Task UpdateAsync(Permission permission, CancellationToken ct = default);

    Task SaveAllAsync(IEnumerable<Permission> permissions, CancellationToken ct = default);
}
