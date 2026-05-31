using AegisIdentity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

internal sealed class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly AegisIdentityDbContext _dbContext;

    public EmailConfirmationTokenRepository(AegisIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmailConfirmationToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => _dbContext.EmailConfirmationTokens
                     .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task InsertAsync(EmailConfirmationToken token, CancellationToken ct = default)
    {
        _dbContext.EmailConfirmationTokens.Add(token);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EmailConfirmationToken token, CancellationToken ct = default)
    {
        _dbContext.EmailConfirmationTokens.Update(token);
        await _dbContext.SaveChangesAsync(ct);
    }
}
