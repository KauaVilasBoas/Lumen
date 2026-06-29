using Lumen.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lumen.Modules.Audit.Persistence;

internal sealed class AuditRepository
{
    private readonly AuditDbContext _dbContext;

    public AuditRepository(AuditDbContext dbContext)
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
