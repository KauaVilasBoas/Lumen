using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Modularity;
using MediatR;

namespace Lumen.Authorization.Application.Profiles.Delete;

public sealed record DeleteProfileCommand(Guid Id) : IRequest;

internal sealed class DeleteProfileCommandHandler
    : IRequestHandler<DeleteProfileCommand>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IEventBus _eventBus;

    public DeleteProfileCommandHandler(
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository,
        IEventBus eventBus)
    {
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(DeleteProfileCommand cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.Id, ct)
            ?? throw new AuthorizationNotFoundException($"Profile '{cmd.Id}' not found.");

        if (profile.IsSystem)
            throw new AuthorizationForbiddenException($"System profile '{profile.Name}' cannot be deleted.");

        var affectedUserIds = await _profileRepository.GetUserIdsByProfileIdAsync(cmd.Id, ct);

        var permissionProfiles = await _profileRepository.GetActivePermissionProfilesByProfileIdAsync(cmd.Id, ct);
        foreach (var pp in permissionProfiles)
            pp.SoftDelete();

        var userProfiles = await _userProfileRepository.ListByProfileIdAsync(cmd.Id, ct);
        foreach (var up in userProfiles)
            up.SoftDelete();

        profile.SoftDelete();

        await _profileRepository.DeleteWithCascadeAsync(profile, permissionProfiles, userProfiles, ct);

        foreach (var userId in affectedUserIds)
            await _eventBus.PublishAsync(new UserPermissionsChangedEvent(userId), ct);
    }
}
