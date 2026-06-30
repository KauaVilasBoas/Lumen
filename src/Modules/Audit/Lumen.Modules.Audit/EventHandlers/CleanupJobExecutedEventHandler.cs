using Lumen.Modules.Audit.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class CleanupJobExecutedEventHandler : IIntegrationEventHandler<CleanupJobExecutedEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<CleanupJobExecutedEventHandler> _logger;

    public CleanupJobExecutedEventHandler(AuditRepository repository, ILogger<CleanupJobExecutedEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(CleanupJobExecutedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.JobCleanup,
            actor: null,
            target: null,
            message: string.Format(AuditMessageTemplates.CleanupJobExecuted, integrationEvent.JobName, integrationEvent.DeletedCount));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for cleanup job: {JobName}", integrationEvent.JobName);
    }
}
