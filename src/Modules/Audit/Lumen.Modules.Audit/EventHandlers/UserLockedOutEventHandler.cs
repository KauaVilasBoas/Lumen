using Lumen.Modules.Audit.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class UserLockedOutEventHandler : IIntegrationEventHandler<UserLockedOutEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<UserLockedOutEventHandler> _logger;

    public UserLockedOutEventHandler(AuditRepository repository, ILogger<UserLockedOutEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(UserLockedOutEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.AuthLockout,
            actor: null,
            target: integrationEvent.Username,
            message: string.Format(AuditMessageTemplates.UserLockedOut, integrationEvent.Username));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for lockout: {UserId}", integrationEvent.UserId);
    }
}
