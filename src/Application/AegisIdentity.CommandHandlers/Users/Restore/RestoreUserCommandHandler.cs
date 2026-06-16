using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Users.Restore;

public sealed class RestoreUserCommandHandler
    : IRequestHandler<RestoreUserCommandHandler.Command>
{
    public sealed record Command(Guid UserId, string ActorId) : IRequest;

    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<RestoreUserCommandHandler> _logger;

    public RestoreUserCommandHandler(
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        ILogger<RestoreUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdIgnoringFiltersAsync(cmd.UserId, ct)
            ?? throw new NotFoundException(AuthErrorMessages.UserNotFound);

        if (!user.IsDeleted)
            throw new NotFoundException(AuthErrorMessages.UserNotDeleted);

        if (IsRestoreWindowExpired(user.DeletedAt))
            throw new ConflictException(AuthErrorMessages.UserRestoreWindowExpired);

        user.Restore();
        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation(
            "User {UserId} restored by actor {ActorId}",
            user.Id, cmd.ActorId);

        var auditEntry = AuditEntry.Create(
            kind: AuditEventKinds.UserRestored,
            actor: cmd.ActorId,
            target: user.Id.ToString(),
            message: $"User '{user.Username}' ({user.Email}) restored from soft delete.");

        await _auditRepository.InsertAsync(auditEntry, ct);
    }

    private static bool IsRestoreWindowExpired(DateTime? deletedAt)
        => deletedAt is null
        || deletedAt.Value.AddDays(ValidationLimits.UserRestoreWindowDays) <= DateTime.UtcNow;
}
