using AegisIdentity.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace AegisIdentity.DataAccess.Persistence.Repositories;

internal sealed class AuditRepository : IAuditRepository
{
    private readonly AegisIdentityDbContext _dbContext;

    public AuditRepository(AegisIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InsertAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _dbContext.AuditEntries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int take, CancellationToken ct = default)
        => _dbContext.AuditEntries
                     .OrderByDescending(a => a.OccurredAt)
                     .Take(take)
                     .ToListAsync(ct)
                     .ContinueWith(
                         t => (IReadOnlyList<AuditEntry>)t.Result,
                         TaskContinuationOptions.ExecuteSynchronously);
}
