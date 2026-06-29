namespace Lumen.Modules.Identity.Domain.Authorization;

internal interface IGroupPermissionRepository
{
    Task<GroupPermission?> FindByNameAsync(string name, CancellationToken ct = default);

    Task<IReadOnlyList<GroupPermission>> ListAllAsync(CancellationToken ct = default);

    Task InsertAsync(GroupPermission groupPermission, CancellationToken ct = default);
}
