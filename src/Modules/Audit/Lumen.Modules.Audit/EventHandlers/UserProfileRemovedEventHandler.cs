using Lumen.Authorization.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class UserProfileRemovedEventHandler : IIntegrationEventHandler<UserProfileRemovedEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<UserProfileRemovedEventHandler> _logger;

    public UserProfileRemovedEventHandler(AuditRepository repository, ILogger<UserProfileRemovedEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(UserProfileRemovedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.UserProfileRemove,
            actor: null,
            target: integrationEvent.Username,
            message: string.Format(AuditMessageTemplates.UserProfileRemoved, integrationEvent.ProfileName, integrationEvent.Username));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug(
            "Audit entry recorded for profile removal: user={UserId}, profile={ProfileId}",
            integrationEvent.UserId, integrationEvent.ProfileId);
    }
}
