using Lumen.Authorization.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class UserProfileAssignedEventHandler : IIntegrationEventHandler<UserProfileAssignedEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<UserProfileAssignedEventHandler> _logger;

    public UserProfileAssignedEventHandler(AuditRepository repository, ILogger<UserProfileAssignedEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(UserProfileAssignedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.UserProfileAssign,
            actor: null,
            target: integrationEvent.Username,
            message: string.Format(AuditMessageTemplates.UserProfileAssigned, integrationEvent.ProfileName, integrationEvent.Username));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug(
            "Audit entry recorded for profile assignment: user={UserId}, profile={ProfileId}",
            integrationEvent.UserId, integrationEvent.ProfileId);
    }
}
