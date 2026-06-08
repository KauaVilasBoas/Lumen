using AegisIdentity.Domain.Audit;
using MediatR;

namespace AegisIdentity.ReadModels.Queries;

public sealed class GetRecentAuditFeedQueryHandler
    : IRequestHandler<GetRecentAuditFeedQueryHandler.Query, IReadOnlyList<GetRecentAuditFeedQueryHandler.AuditEntryResult>>
{
    private const int MinTake = 1;
    private const int MaxTake = 100;
    private const int DefaultTake = 20;

    public sealed record Query(int Take = DefaultTake)
        : IRequest<IReadOnlyList<AuditEntryResult>>;

    public sealed record AuditEntryResult(
        string Kind,
        string? Actor,
        string? Target,
        string Message,
        DateTime OccurredAt);

    private readonly IAuditRepository _auditRepository;

    public GetRecentAuditFeedQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<IReadOnlyList<AuditEntryResult>> Handle(Query query, CancellationToken ct)
    {
        var take = ClampTake(query.Take);

        var entries = await _auditRepository.GetRecentAsync(take, ct);

        return entries
            .Select(e => new AuditEntryResult(
                Kind: e.Kind,
                Actor: e.Actor,
                Target: e.Target,
                Message: e.Message,
                OccurredAt: e.OccurredAt))
            .ToList();
    }

    private static int ClampTake(int requested)
        => Math.Clamp(requested, MinTake, MaxTake);
}
