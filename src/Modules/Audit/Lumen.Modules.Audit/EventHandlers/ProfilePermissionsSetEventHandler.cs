using Lumen.Modules.Audit.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class ProfilePermissionsSetEventHandler : IIntegrationEventHandler<ProfilePermissionsSetEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<ProfilePermissionsSetEventHandler> _logger;

    public ProfilePermissionsSetEventHandler(AuditRepository repository, ILogger<ProfilePermissionsSetEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(ProfilePermissionsSetEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.ProfilePermSet,
            actor: integrationEvent.ActorUsername,
            target: integrationEvent.ProfileName,
            message: string.Format(AuditMessageTemplates.ProfilePermissionsUpdated, integrationEvent.ProfileName, integrationEvent.ActorUsername));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for profile permissions set: {ProfileId}", integrationEvent.ProfileId);
    }
}
