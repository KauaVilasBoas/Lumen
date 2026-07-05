using Lumen.Authorization;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Contracts.Events;
using Lumen.Modularity;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Users.Delete;

public sealed record DeleteUserCommand(Guid UserId, string ActorId) : IRequest;

internal sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEventBus _eventBus;
    private readonly IUserProfileGuard _userProfileGuard;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEventBus eventBus,
        IUserProfileGuard userProfileGuard,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _eventBus = eventBus;
        _userProfileGuard = userProfileGuard;
        _logger = logger;
    }

    public async Task Handle(DeleteUserCommand cmd, CancellationToken ct)
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

        await _eventBus.PublishAsync(new UserPermissionsChangedEvent(user.Id), ct);
    }

    private async Task GuardLastAdministratorAsync(Guid userId, CancellationToken ct)
    {
        var isAdministrator = await _userProfileGuard.IsUserAdministratorAsync(userId, ct);

        if (!isAdministrator)
            return;

        var activeAdminCount = await _userProfileGuard.CountActiveAdministratorsAsync(
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
