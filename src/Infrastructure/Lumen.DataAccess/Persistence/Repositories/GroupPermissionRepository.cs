using Lumen.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Persistence.Repositories;

internal sealed class GroupPermissionRepository : IGroupPermissionRepository
{
    private readonly LumenDbContext _dbContext;

    public GroupPermissionRepository(LumenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<GroupPermission?> FindByNameAsync(string name, CancellationToken ct = default)
        => _dbContext.GroupPermissions
                     .FirstOrDefaultAsync(g => g.Name == name, ct);

    public async Task<IReadOnlyList<GroupPermission>> ListAllAsync(CancellationToken ct = default)
        => await _dbContext.GroupPermissions.ToListAsync(ct);

    public async Task InsertAsync(GroupPermission groupPermission, CancellationToken ct = default)
    {
        _dbContext.GroupPermissions.Add(groupPermission);
        await _dbContext.SaveChangesAsync(ct);
    }
}
