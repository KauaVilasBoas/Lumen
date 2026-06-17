using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using MediatR;

namespace AegisIdentity.CommandHandlers.Profiles.DeleteProfile;

public sealed class DeleteProfileCommandHandler
    : IRequestHandler<DeleteProfileCommandHandler.Command>
{
    public sealed record Command(Guid Id) : IRequest;

    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;

    public DeleteProfileCommandHandler(
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository,
        IPublisher publisher)
    {
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
        _publisher = publisher;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(string.Format(ProfileErrorMessages.ProfileNotFound, cmd.Id));

        if (profile.IsSystem)
            throw new ForbiddenException(string.Format(ProfileErrorMessages.SystemProfileCannotDelete, profile.Name));

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
            await _publisher.Publish(new UserPermissionsChanged(userId), ct);
    }
}
