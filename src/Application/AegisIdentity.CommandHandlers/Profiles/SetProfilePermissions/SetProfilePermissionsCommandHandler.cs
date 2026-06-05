using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.Profiles.SetProfilePermissions;

public sealed class SetProfilePermissionsCommandHandler
    : IRequestHandler<SetProfilePermissionsCommandHandler.Command>
{
    public sealed record Command(Guid ProfileId, IReadOnlyList<Guid> PermissionIds) : IRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage("ProfileId is required.");

            RuleFor(x => x.PermissionIds)
                .NotNull().WithMessage("PermissionIds is required.");

            RuleForEach(x => x.PermissionIds)
                .NotEmpty().WithMessage("Each PermissionId must be a valid non-empty Guid.");
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
            ?? throw new NotFoundException($"Profile '{cmd.ProfileId}' not found.");

        if (profile.IsSystem)
            throw new ForbiddenException($"Permissions on system profile '{profile.Name}' are managed automatically and cannot be overwritten via the API.");

        foreach (var permId in cmd.PermissionIds)
        {
            var permission = await _permissionRepository.FindByIdAsync(permId, ct);
            if (permission is null)
                throw new NotFoundException($"Permission '{permId}' not found.");
        }

        var existingAll = await _profileRepository.GetActivePermissionProfilesByProfileIdAsync(cmd.ProfileId, ct);

        var desiredSet = new HashSet<Guid>(cmd.PermissionIds);
        var existingByPermId = existingAll.ToDictionary(pp => pp.PermissionId);

        var toSoftDelete = existingAll
            .Where(pp => !pp.IsDeleted && !desiredSet.Contains(pp.PermissionId))
            .ToList();

        var currentActivePermIds = new HashSet<Guid>(
            existingAll.Where(pp => !pp.IsDeleted).Select(pp => pp.PermissionId));

        var toAdd = desiredSet
            .Where(permId => !currentActivePermIds.Contains(permId))
            .ToList();

        foreach (var pp in toSoftDelete)
        {
            pp.SoftDelete();
            await _profileRepository.UpdatePermissionProfileAsync(pp, ct);
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
    }
}
