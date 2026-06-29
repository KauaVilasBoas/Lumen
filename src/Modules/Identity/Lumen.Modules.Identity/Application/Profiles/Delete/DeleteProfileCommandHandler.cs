using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using MediatR;

namespace Lumen.Modules.Identity.Application.Profiles.Delete;

internal sealed class DeleteProfileCommandHandler
    : IRequestHandler<DeleteProfileCommandHandler.Command>
{
    public sealed record Command(Guid Id) : IRequest;

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

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException($"Profile '{cmd.Id}' not found.");

        if (profile.IsSystem)
            throw new ForbiddenException($"System profile '{profile.Name}' cannot be deleted.");

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
