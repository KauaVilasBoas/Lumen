using Lumen.Authorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lumen.Authorization.Persistence.Repositories;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly LumenAuthorizationDbContext _dbContext;

    public PermissionRepository(LumenAuthorizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Permission?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Permission?> FindByCodeAsync(string code, CancellationToken ct = default)
        => _dbContext.Permissions.FirstOrDefaultAsync(p => p.Code == code, ct);

    public async Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken ct = default)
        => await _dbContext.Permissions.ToListAsync(ct);

    public async Task InsertAsync(Permission permission, CancellationToken ct = default)
    {
        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Permission permission, CancellationToken ct = default)
    {
        _dbContext.Permissions.Update(permission);
        await _dbContext.SaveChangesAsync(ct);
    }
}
