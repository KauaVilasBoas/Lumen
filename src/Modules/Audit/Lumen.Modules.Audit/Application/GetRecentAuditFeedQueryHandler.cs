using FluentValidation;
using Lumen.Modules.Audit.Persistence;
using Lumen.SharedKernel.Constants;
using MediatR;

namespace Lumen.Modules.Audit.Application;

public sealed record GetRecentAuditFeedQuery(int Take = ValidationLimits.AuditTakeDefaultValue)
    : IRequest<IReadOnlyList<AuditEntryResult>>;

public sealed record AuditEntryResult(
    string Kind,
    string? Actor,
    string? Target,
    string Message,
    DateTime OccurredAt);

internal sealed class GetRecentAuditFeedQueryHandler
    : IRequestHandler<GetRecentAuditFeedQuery, IReadOnlyList<AuditEntryResult>>
{
    public sealed class Validator : AbstractValidator<GetRecentAuditFeedQuery>
    {
        public Validator()
        {
            RuleFor(q => q.Take)
                .InclusiveBetween(ValidationLimits.AuditTakeMinValue, ValidationLimits.AuditTakeMaxValue)
                .OverridePropertyName("take");
        }
    }

    private readonly AuditRepository _auditRepository;

    public GetRecentAuditFeedQueryHandler(AuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<IReadOnlyList<AuditEntryResult>> Handle(GetRecentAuditFeedQuery query, CancellationToken ct)
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
