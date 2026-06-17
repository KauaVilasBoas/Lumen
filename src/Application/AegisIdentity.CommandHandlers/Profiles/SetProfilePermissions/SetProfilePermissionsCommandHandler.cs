using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.Profiles.SetProfilePermissions;

public sealed class SetProfilePermissionsCommandHandler
    : IRequestHandler<SetProfilePermissionsCommandHandler.Command>
{
    public sealed record Command(Guid ProfileId, IReadOnlyList<Guid> PermissionIds, string? ActorUsername = null) : IRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage(ProfileErrorMessages.ProfileIdRequired);

            RuleFor(x => x.PermissionIds)
                .NotNull().WithMessage(ProfileErrorMessages.PermissionIdsRequired);

            RuleForEach(x => x.PermissionIds)
                .NotEmpty().WithMessage(ProfileErrorMessages.PermissionIdInvalid);
        }
    }

    private readonly IProfileRepository _profileRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPublisher _publisher;

    public SetProfilePermissionsCommandHandler(
        IProfileRepository profileRepository,
        IPermissionRepository permissionRepository,
        IPublisher publisher)
    {
        _profileRepository = profileRepository;
        _permissionRepository = permissionRepository;
        _publisher = publisher;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new NotFoundException(string.Format(ProfileErrorMessages.ProfileNotFound, cmd.ProfileId));

        if (profile.IsSystem)
            throw new ForbiddenException(string.Format(ProfileErrorMessages.SystemProfilePermissionsReadOnly, profile.Name));

        if (cmd.PermissionIds.Count > 0)
        {
            var foundPermissions = await _permissionRepository.GetByIdsAsync(cmd.PermissionIds, ct);
            if (foundPermissions.Count != cmd.PermissionIds.Count)
            {
                var foundIds = new HashSet<Guid>(foundPermissions.Select(p => p.Id));
                var missingId = cmd.PermissionIds.First(id => !foundIds.Contains(id));
                throw new NotFoundException(string.Format(ProfileErrorMessages.PermissionNotFound, missingId));
            }
        }

        var existingAll = await _profileRepository.GetActivePermissionProfilesByProfileIdAsync(cmd.ProfileId, ct);

        var desiredSet = new HashSet<Guid>(cmd.PermissionIds);

        var toSoftDelete = existingAll
            .Where(pp => !pp.IsDeleted && !desiredSet.Contains(pp.PermissionId))
            .ToList();

        var currentActivePermIds = new HashSet<Guid>(
            existingAll.Where(pp => !pp.IsDeleted).Select(pp => pp.PermissionId));

        var toAdd = desiredSet
            .Where(permId => !currentActivePermIds.Contains(permId))
            .ToList();

        if (toSoftDelete.Count > 0)
        {
            foreach (var pp in toSoftDelete)
                pp.SoftDelete();

            await _profileRepository.UpdatePermissionProfilesAsync(toSoftDelete, ct);
        }

        if (toAdd.Count > 0)
        {
            var newAssignments = toAdd
                .Select(permId => PermissionProfile.Create(permId, cmd.ProfileId))
                .ToList();
            await _profileRepository.InsertPermissionProfilesAsync(newAssignments, ct);
        }

        var affectedUserIds = await _profileRepository.GetUserIdsByProfileIdAsync(cmd.ProfileId, ct);
        foreach (var userId in affectedUserIds)
            await _publisher.Publish(new UserPermissionsChanged(userId), ct);

        await _publisher.Publish(new ProfilePermissionsSet(cmd.ProfileId, profile.Name, cmd.ActorUsername ?? "system"), ct);
    }
}
