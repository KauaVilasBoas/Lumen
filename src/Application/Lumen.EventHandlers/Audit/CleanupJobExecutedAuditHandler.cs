using AegisIdentity.Domain.Audit;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

public sealed class CleanupJobExecutedAuditHandler : INotificationHandler<CleanupJobExecuted>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<CleanupJobExecutedAuditHandler> _logger;

    public CleanupJobExecutedAuditHandler(
        IAuditRepository auditRepository,
        ILogger<CleanupJobExecutedAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(CleanupJobExecuted notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.JobCleanup,
            actor: null,
            target: null,
            message: string.Format(AuditMessageTemplates.CleanupJobExecuted, notification.JobName, notification.DeletedCount));

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for cleanup job: {JobName}", notification.JobName);
    }
}
