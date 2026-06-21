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

        // Guard from FIX-01: checked before any mutation or transaction begins.
        if (profile.IsSystem)
            throw new ForbiddenException($"System profile '{profile.Name}' cannot be deleted.");

        // Collect affected user IDs before any soft-delete so we know whose
        // permission cache to invalidate after the transaction commits.
        var affectedUserIds = await _profileRepository.GetUserIdsByProfileIdAsync(cmd.Id, ct);

        // Mark all dependents as soft-deleted in memory (no DB write yet).
        var permissionProfiles = await _profileRepository.GetActivePermissionProfilesByProfileIdAsync(cmd.Id, ct);
        foreach (var pp in permissionProfiles)
            pp.SoftDelete();

        var userProfiles = await _userProfileRepository.ListByProfileIdAsync(cmd.Id, ct);
        foreach (var up in userProfiles)
            up.SoftDelete();

        profile.SoftDelete();

        // Persist everything atomically: children first, then profile.
        // If any step fails the repository rolls back the transaction and re-throws,
        // leaving the database in its original state (no partial deletes).
        await _profileRepository.DeleteWithCascadeAsync(profile, permissionProfiles, userProfiles, ct);

        // Cache invalidation runs after the transaction commits successfully.
        // Events are published per-user so each user's Redis entry is evicted.
        foreach (var userId in affectedUserIds)
            await _publisher.Publish(new UserPermissionsChanged(userId), ct);
    }
}
