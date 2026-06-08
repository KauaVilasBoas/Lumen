using AegisIdentity.Domain.Audit;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

public sealed class UserProfileAssignedAuditHandler : INotificationHandler<UserProfileAssigned>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UserProfileAssignedAuditHandler> _logger;

    public UserProfileAssignedAuditHandler(
        IAuditRepository auditRepository,
        ILogger<UserProfileAssignedAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(UserProfileAssigned notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.UserProfileAssign,
            actor: null,
            target: notification.Username,
            message: $"Profile '{notification.ProfileName}' assigned to user '{notification.Username}'.");

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug(
            "Audit entry recorded for profile assignment: user={UserId}, profile={ProfileId}",
            notification.UserId, notification.ProfileId);
    }
}
