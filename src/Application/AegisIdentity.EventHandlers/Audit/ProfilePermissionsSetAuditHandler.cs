using AegisIdentity.Domain.Audit;
using AegisIdentity.SharedKernel.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.EventHandlers.Audit;

public sealed class ProfilePermissionsSetAuditHandler : INotificationHandler<ProfilePermissionsSet>
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<ProfilePermissionsSetAuditHandler> _logger;

    public ProfilePermissionsSetAuditHandler(
        IAuditRepository auditRepository,
        ILogger<ProfilePermissionsSetAuditHandler> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(ProfilePermissionsSet notification, CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            kind: AuditEventKinds.ProfilePermSet,
            actor: notification.ActorUsername,
            target: notification.ProfileName,
            message: $"Permissions updated on profile '{notification.ProfileName}' by '{notification.ActorUsername}'.");

        await _auditRepository.InsertAsync(entry, cancellationToken);

        _logger.LogDebug("Audit entry recorded for profile permissions set: {ProfileId}", notification.ProfileId);
    }
}
