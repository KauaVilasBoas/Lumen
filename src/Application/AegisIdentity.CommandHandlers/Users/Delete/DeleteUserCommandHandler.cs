using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Users.Delete;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommandHandler.Command>
{
    public sealed record Command(Guid UserId, string ActorId) : IRequest;

    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditRepository auditRepository,
        IPublisher publisher,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _auditRepository = auditRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException(AuthErrorMessages.UserNotFound);

        if (user.IsBootstrap)
            throw new ForbiddenException(AuthErrorMessages.CannotDeleteBootstrapUser);

        await GuardLastAdministratorAsync(cmd.UserId, ct);

        user.SoftDelete();
        await _userRepository.UpdateAsync(user, ct);

        await RevokeActiveRefreshTokensAsync(user.Id, ct);

        _logger.LogInformation(
            "User {UserId} soft-deleted by actor {ActorId}",
            user.Id, cmd.ActorId);

        var auditEntry = AuditEntry.Create(
            kind: AuditEventKinds.UserDeleted,
            actor: cmd.ActorId,
            target: user.Id.ToString(),
            message: $"User '{user.Username}' ({user.Email}) soft-deleted.");

        await _auditRepository.InsertAsync(auditEntry, ct);
        await _publisher.Publish(new UserPermissionsChanged(user.Id), ct);
    }

    private async Task GuardLastAdministratorAsync(Guid userId, CancellationToken ct)
    {
        var isAdministrator = await _userProfileRepository.FindActiveAsync(
            userId, SystemProfiles.AdministratorId, ct) is not null;

        if (!isAdministrator)
            return;

        var activeAdminCount = await _userRepository.CountActiveAdministratorsAsync(
            SystemProfiles.AdministratorId, ct);

        if (activeAdminCount <= 1)
            throw new ConflictException(AuthErrorMessages.CannotDeleteLastAdministrator);
    }

    private async Task RevokeActiveRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var tokens = await _refreshTokenRepository.FindByUserIdAsync(userId, ct);

        foreach (var token in tokens.Where(t => t.IsActive()))
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token, ct);
        }
    }
}
