using AegisIdentity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AegisIdentityDbContext _dbContext;

    public PasswordResetTokenRepository(AegisIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => _dbContext.PasswordResetTokens
                     .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task InsertAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _dbContext.PasswordResetTokens.Add(token);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _dbContext.PasswordResetTokens.Update(token);
        await _dbContext.SaveChangesAsync(ct);
    }
}
