using AegisIdentity.Domain.Audit;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

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
            message: $"Account '{notification.Username}' locked out after repeated failed login attempts.");

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for lockout: {UserId}", notification.UserId);
    }
}
