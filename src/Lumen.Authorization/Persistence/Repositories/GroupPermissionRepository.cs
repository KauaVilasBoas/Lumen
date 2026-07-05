using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lumen.Authorization.Persistence.Repositories;

internal sealed class GroupPermissionRepository : IGroupPermissionRepository
{
    private readonly LumenAuthorizationDbContext _dbContext;

    public GroupPermissionRepository(LumenAuthorizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GroupPermission?> FindByNameAsync(string name, CancellationToken ct = default)
        => _dbContext.GroupPermissions.FirstOrDefaultAsync(g => g.Name == name, ct);

    public async Task<IReadOnlyList<GroupPermission>> ListAllAsync(CancellationToken ct = default)
        => await _dbContext.GroupPermissions.ToListAsync(ct);

    public async Task InsertAsync(GroupPermission groupPermission, CancellationToken ct = default)
    {
        _dbContext.GroupPermissions.Add(groupPermission);
        await _dbContext.SaveChangesAsync(ct);
    }
}
