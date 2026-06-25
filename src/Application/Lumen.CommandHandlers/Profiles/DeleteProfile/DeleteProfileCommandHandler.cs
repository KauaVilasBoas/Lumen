using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using MediatR;

namespace Lumen.CommandHandlers.Profiles.DeleteProfile;

public sealed class DeleteProfileCommandHandler
    : IRequestHandler<DeleteProfileCommandHandler.Command>
{
    public sealed record Command(Guid Id) : IRequest;

    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public DeleteProfileCommandHandler(
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository)
    {
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
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

        profile.Delete(affectedUserIds);

        await _profileRepository.DeleteWithCascadeAsync(profile, permissionProfiles, userProfiles, ct);
    }
}
