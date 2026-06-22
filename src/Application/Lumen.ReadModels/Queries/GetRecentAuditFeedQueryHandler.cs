using Lumen.Domain.Audit;
using Lumen.SharedKernel.Constants;
using FluentValidation;
using MediatR;

namespace Lumen.ReadModels.Queries;

public sealed class GetRecentAuditFeedQueryHandler
    : IRequestHandler<GetRecentAuditFeedQueryHandler.Query, IReadOnlyList<GetRecentAuditFeedQueryHandler.AuditEntryResult>>
{
    public sealed record Query(int Take = ValidationLimits.AuditTakeDefaultValue)
        : IRequest<IReadOnlyList<AuditEntryResult>>;

    public sealed record AuditEntryResult(
        string Kind,
        string? Actor,
        string? Target,
        string Message,
        DateTime OccurredAt);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.Take)
                .InclusiveBetween(ValidationLimits.AuditTakeMinValue, ValidationLimits.AuditTakeMaxValue)
                .OverridePropertyName("take");
        }
    }

    private readonly IAuditRepository _auditRepository;

    public GetRecentAuditFeedQueryHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<IReadOnlyList<AuditEntryResult>> Handle(Query query, CancellationToken ct)
    {
        var entries = await _auditRepository.GetRecentAsync(query.Take, ct);

        return entries
            .Select(e => new AuditEntryResult(
                Kind: e.Kind,
                Actor: e.Actor,
                Target: e.Target,
                Message: e.Message,
                OccurredAt: e.OccurredAt))
            .ToList();
    }
}
