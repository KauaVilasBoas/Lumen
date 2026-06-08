namespace AegisIdentity.Domain.Audit;

public interface IAuditRepository
{
    Task InsertAsync(AuditEntry entry, CancellationToken ct = default);

    Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int take, CancellationToken ct = default);
}
