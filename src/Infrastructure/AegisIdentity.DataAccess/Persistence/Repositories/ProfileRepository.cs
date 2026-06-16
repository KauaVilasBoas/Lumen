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

    public Task<bool> ActiveNameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
        => _dbContext.Profiles
                     .AnyAsync(p => p.Name == name && (excludeId == null || p.Id != excludeId.Value), ct);

    public async Task<IReadOnlyList<Domain.Authorization.Profile>> ListAllAsync(CancellationToken ct = default)
        => await _dbContext.Profiles.ToListAsync(ct);

    public async Task<IReadOnlyList<Domain.Authorization.Profile>> GetProfilesByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
        => await _dbContext.UserProfiles
                           .Where(up => up.UserId == userId)
                           .Join(
                               _dbContext.Profiles,
                               up => up.ProfileId,
                               p => p.Id,
                               (up, p) => p)
                           .AsNoTracking()
                           .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Domain.Authorization.Profile>>> GetProfilesByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct = default)
    {
        var rows = await _dbContext.UserProfiles
            .Where(up => userIds.Contains(up.UserId))
            .Join(
                _dbContext.Profiles,
                up => up.ProfileId,
                p => p.Id,
                (up, p) => new { up.UserId, Profile = p })
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Domain.Authorization.Profile>)g.Select(r => r.Profile).ToList());
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetPermissionCountsByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct = default)
    {
        var pairs = await _dbContext.UserProfiles
            .Where(up => userIds.Contains(up.UserId))
            .Join(
                _dbContext.PermissionProfiles,
                up => up.ProfileId,
                pp => pp.ProfileId,
                (up, pp) => new { up.UserId, pp.PermissionId })
            .Join(
                _dbContext.Permissions,
                r => r.PermissionId,
                p => p.Id,
                (r, p) => new { r.UserId, r.PermissionId })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        return pairs
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetPermissionCountsByProfileIdsAsync(
        IReadOnlyList<Guid> profileIds,
        CancellationToken ct = default)
    {
        var counts = await _dbContext.PermissionProfiles
            .Where(pp => profileIds.Contains(pp.ProfileId))
            .GroupBy(pp => pp.ProfileId)
            .Select(g => new { ProfileId = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToListAsync(ct);

        return counts.ToDictionary(r => r.ProfileId, r => r.Count);
    }

    public async Task<IReadOnlyList<Domain.Authorization.Profile>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default)
        => await _dbContext.Profiles
                           .Where(p => ids.Contains(p.Id))
                           .ToListAsync(ct);

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

    public async Task<IReadOnlyList<PermissionProfile>> GetActivePermissionProfilesByProfileIdAsync(
        Guid profileId,
        CancellationToken ct = default)
        => await _dbContext.PermissionProfiles
                           .Where(pp => pp.ProfileId == profileId)
                           .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PermissionProfile>>> GetActivePermissionProfilesByProfileIdsAsync(
        IReadOnlyList<Guid> profileIds,
        CancellationToken ct = default)
    {
        var rows = await _dbContext.PermissionProfiles
            .Where(pp => profileIds.Contains(pp.ProfileId))
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .GroupBy(pp => pp.ProfileId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PermissionProfile>)g.ToList());
    }

    public async Task InsertPermissionProfilesAsync(
        IReadOnlyList<PermissionProfile> permissionProfiles,
        CancellationToken ct = default)
    {
        _dbContext.PermissionProfiles.AddRange(permissionProfiles);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdatePermissionProfileAsync(
        PermissionProfile permissionProfile,
        CancellationToken ct = default)
    {
        _dbContext.PermissionProfiles.Update(permissionProfile);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsByProfileIdAsync(
        Guid profileId,
        CancellationToken ct = default)
        => await _dbContext.UserProfiles
                           .Where(up => up.ProfileId == profileId)
                           .Select(up => up.UserId)
                           .ToListAsync(ct);

    public async Task DeleteWithCascadeAsync(
        Domain.Authorization.Profile profile,
        IReadOnlyList<PermissionProfile> permissionProfiles,
        IReadOnlyList<UserProfile> userProfiles,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            _dbContext.PermissionProfiles.UpdateRange(permissionProfiles);
            _dbContext.UserProfiles.UpdateRange(userProfiles);
            _dbContext.Profiles.Update(profile);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
