using Lumen.Authorization.Contracts.Events;
using Lumen.Modules.Audit.Domain;
using Lumen.Modules.Audit.Persistence;
using Lumen.Modularity;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Audit.EventHandlers;

internal sealed class UserPermissionsChangedEventHandler : IIntegrationEventHandler<UserPermissionsChangedEvent>
{
    private readonly AuditRepository _repository;
    private readonly ILogger<UserPermissionsChangedEventHandler> _logger;

    public UserPermissionsChangedEventHandler(AuditRepository repository, ILogger<UserPermissionsChangedEventHandler> logger)
    {
        _repository = repository;
        _logger     = logger;
    }

    public async Task HandleAsync(UserPermissionsChangedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.CacheInvalidate,
            actor: null,
            target: integrationEvent.UserId.ToString(),
            message: string.Format(AuditMessageTemplates.UserPermissionCacheInvalidated, integrationEvent.UserId));

        await _repository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for permission cache invalidation: {UserId}", integrationEvent.UserId);
    }
}
