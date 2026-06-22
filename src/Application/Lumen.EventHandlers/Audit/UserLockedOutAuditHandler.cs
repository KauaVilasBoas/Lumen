using Lumen.Domain.Audit;
using Lumen.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.EventHandlers.Audit;

public sealed class UserLockedOutAuditHandler : INotificationHandler<UserLockedOut>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UserLockedOutAuditHandler> _logger;

    public UserLockedOutAuditHandler(
        IAuditRepository auditRepository,
        ILogger<UserLockedOutAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(UserLockedOut notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.AuthLockout,
            actor: null,
            target: notification.Username,
            message: string.Format(AuditMessageTemplates.UserLockedOut, notification.Username));

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for lockout: {UserId}", notification.UserId);
    }
}
