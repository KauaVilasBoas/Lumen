using AegisIdentity.Domain.Audit;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

public sealed class UserProfileRemovedAuditHandler : INotificationHandler<UserProfileRemoved>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UserProfileRemovedAuditHandler> _logger;

    public UserProfileRemovedAuditHandler(
        IAuditRepository auditRepository,
        ILogger<UserProfileRemovedAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(UserProfileRemoved notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.UserProfileRemove,
            actor: null,
            target: notification.Username,
            message: $"Profile '{notification.ProfileName}' removed from user '{notification.Username}'.");

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug(
            "Audit entry recorded for profile removal: user={UserId}, profile={ProfileId}",
            notification.UserId, notification.ProfileId);
    }
}
