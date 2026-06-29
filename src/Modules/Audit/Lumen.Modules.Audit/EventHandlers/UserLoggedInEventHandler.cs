using Lumen.Modules.Audit.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class UserLoggedInEventHandler : IIntegrationEventHandler<UserLoggedInEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<UserLoggedInEventHandler> _logger;

    public UserLoggedInEventHandler(AuditRepository repository, ILogger<UserLoggedInEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(UserLoggedInEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.AuthLogin,
            actor: integrationEvent.Username,
            target: null,
            message: string.Format(AuditMessageTemplates.UserLoggedIn, integrationEvent.Username));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for user login: {UserId}", integrationEvent.UserId);
    }
}
