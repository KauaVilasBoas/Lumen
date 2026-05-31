using AegisIdentity.Domain.Authorization;
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
            ?? throw new NotFoundException($"Profile '{cmd.Id}' not found.");

        var affectedUserIds = await _profileRepository.GetUserIdsByProfileIdAsync(cmd.Id, ct);

        var permissionProfiles = await _profileRepository.GetActivePermissionProfilesByProfileIdAsync(cmd.Id, ct);
        foreach (var pp in permissionProfiles)
        {
            pp.SoftDelete();
            await _profileRepository.UpdatePermissionProfileAsync(pp, ct);
        }

        var userProfiles = await _userProfileRepository.ListByProfileIdAsync(cmd.Id, ct);
        foreach (var up in userProfiles)
        {
            up.SoftDelete();
            await _userProfileRepository.UpdateAsync(up, ct);
        }

        profile.SoftDelete();
        await _profileRepository.UpdateAsync(profile, ct);

        foreach (var userId in affectedUserIds)
            await _publisher.Publish(new UserPermissionsChanged(userId), ct);
    }
}
