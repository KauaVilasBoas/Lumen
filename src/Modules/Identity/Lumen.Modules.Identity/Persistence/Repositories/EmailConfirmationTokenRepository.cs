using Lumen.Modules.Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Lumen.Modules.Identity.Persistence.Repositories;

internal sealed class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public EmailConfirmationTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmailConfirmationToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => _dbContext.EmailConfirmationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

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

    public async Task InvalidateByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var activeTokens = await _dbContext.EmailConfirmationTokens
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.SoftDelete();

        await _dbContext.SaveChangesAsync(ct);
    }
}
