using AegisIdentity.Domain.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

internal sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AegisIdentityDbContext _dbContext;

    public UserProfileRepository(AegisIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserProfile?> FindActiveAsync(Guid userId, Guid profileId, CancellationToken ct = default)
        => _dbContext.UserProfiles
                     .FirstOrDefaultAsync(up => up.UserId == userId && up.ProfileId == profileId, ct);

    public async Task<IReadOnlyList<UserProfile>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _dbContext.UserProfiles
                           .Where(up => up.UserId == userId)
                           .ToListAsync(ct);

    public async Task<IReadOnlyList<UserProfile>> ListByProfileIdAsync(Guid profileId, CancellationToken ct = default)
        => await _dbContext.UserProfiles
                           .Where(up => up.ProfileId == profileId)
                           .ToListAsync(ct);

    public async Task InsertAsync(UserProfile userProfile, CancellationToken ct = default)
    {
        _dbContext.UserProfiles.Add(userProfile);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserProfile userProfile, CancellationToken ct = default)
    {
        _dbContext.UserProfiles.Update(userProfile);
        await _dbContext.SaveChangesAsync(ct);
    }
}
