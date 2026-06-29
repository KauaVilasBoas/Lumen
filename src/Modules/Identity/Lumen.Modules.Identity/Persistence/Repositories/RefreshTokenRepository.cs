using Lumen.Modules.Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Lumen.Modules.Identity.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var results = await _dbContext.RefreshTokens
                                      .IgnoreQueryFilters()
                                      .Where(t => t.UserId == userId)
                                      .ToListAsync(ct);
        return results.AsReadOnly();
    }

    public async Task InsertAsync(RefreshToken token, CancellationToken ct = default)
    {
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _dbContext.RefreshTokens.Update(token);
        await _dbContext.SaveChangesAsync(ct);
    }
}
