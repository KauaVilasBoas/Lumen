using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Users.Restore;

public sealed record RestoreUserCommand(Guid UserId, string ActorId) : IRequest;

internal sealed class RestoreUserCommandHandler
    : IRequestHandler<RestoreUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RestoreUserCommandHandler> _logger;

    public RestoreUserCommandHandler(
        IUserRepository userRepository,
        ILogger<RestoreUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(RestoreUserCommand cmd, CancellationToken ct)
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
    }

    private static bool IsRestoreWindowExpired(DateTime? deletedAt)
        => deletedAt is null
        || deletedAt.Value.AddDays(ValidationLimits.UserRestoreWindowDays) <= DateTime.UtcNow;
}
