using AegisIdentity.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

internal sealed class ProfileRepository : IProfileRepository
{
    private readonly AegisIdentityDbContext _dbContext;

    public ProfileRepository(AegisIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Domain.Authorization.Profile?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Profiles
                     .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Domain.Authorization.Profile?> FindByNameAsync(string name, CancellationToken ct = default)
        => _dbContext.Profiles
                     .FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<IReadOnlyList<Domain.Authorization.Profile>> ListAllAsync(CancellationToken ct = default)
        => await _dbContext.Profiles.ToListAsync(ct);

    public async Task InsertAsync(Domain.Authorization.Profile profile, CancellationToken ct = default)
    {
        _dbContext.Profiles.Add(profile);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Domain.Authorization.Profile profile, CancellationToken ct = default)
    {
        _dbContext.Profiles.Update(profile);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<HashSet<string>> GetPermissionCodesByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var codes = await _dbContext.UserProfiles
            .Where(up => up.UserId == userId)
            .Join(
                _dbContext.PermissionProfiles,
                up => up.ProfileId,
                pp => pp.ProfileId,
                (up, pp) => pp.PermissionId)
            .Join(
                _dbContext.Permissions,
                permissionId => permissionId,
                p => p.Id,
                (permissionId, p) => p.Code)
            .ToListAsync(ct);

        return [.. codes];
    }

    public async Task<IReadOnlyList<PermissionProfile>> GetPermissionProfilesByProfileIdAsync(
        Guid profileId,
        CancellationToken ct = default)
        => await _dbContext.PermissionProfiles
                           .Where(pp => pp.ProfileId == profileId && !pp.IsDeleted)
                           .ToListAsync(ct);

    public async Task InsertPermissionProfilesAsync(
        IReadOnlyList<PermissionProfile> permissionProfiles,
        CancellationToken ct = default)
    {
        _dbContext.PermissionProfiles.AddRange(permissionProfiles);
        await _dbContext.SaveChangesAsync(ct);
    }
}
