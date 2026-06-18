using AegisIdentity.Domain.Audit;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

public sealed class UserLoggedInAuditHandler : INotificationHandler<UserLoggedIn>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UserLoggedInAuditHandler> _logger;

    public UserLoggedInAuditHandler(
        IAuditRepository auditRepository,
        ILogger<UserLoggedInAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(UserLoggedIn notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.AuthLogin,
            actor: notification.Username,
            target: null,
            message: string.Format(AuditMessageTemplates.UserLoggedIn, notification.Username));

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for user login: {UserId}", notification.UserId);
    }
}
