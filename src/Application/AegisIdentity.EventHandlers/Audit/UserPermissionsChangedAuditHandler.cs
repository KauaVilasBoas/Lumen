using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

public sealed class UserPermissionsChangedAuditHandler : INotificationHandler<UserPermissionsChanged>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UserPermissionsChangedAuditHandler> _logger;

    public UserPermissionsChangedAuditHandler(
        IAuditRepository auditRepository,
        ILogger<UserPermissionsChangedAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(UserPermissionsChanged notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.CacheInvalidate,
            actor: null,
            target: notification.UserId.ToString(),
            message: string.Format(AuditMessageTemplates.UserPermissionCacheInvalidated, notification.UserId));

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for permission cache invalidation: {UserId}", notification.UserId);
    }
}
