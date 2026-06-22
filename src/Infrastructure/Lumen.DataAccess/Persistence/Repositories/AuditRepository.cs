using Lumen.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess.Persistence.Repositories;

internal sealed class AuditRepository : IAuditRepository
{
    private readonly LumenDbContext _dbContext;

    public AuditRepository(LumenDbContext dbContext)
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
