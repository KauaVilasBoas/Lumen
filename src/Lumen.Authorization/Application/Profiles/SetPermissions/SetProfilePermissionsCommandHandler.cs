using FluentValidation;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Authorization.Internal;
using Lumen.Modularity;
using MediatR;

namespace Lumen.Authorization.Application.Profiles.SetPermissions;

public sealed record SetProfilePermissionsCommand(Guid ProfileId, IReadOnlyList<Guid> PermissionIds, string? ActorUsername = null) : IRequest;

internal sealed class SetProfilePermissionsCommandHandler
    : IRequestHandler<SetProfilePermissionsCommand>
{
    public sealed class Validator : AbstractValidator<SetProfilePermissionsCommand>
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
    private readonly IEventBus _eventBus;

    public SetProfilePermissionsCommandHandler(
        IProfileRepository profileRepository,
        IPermissionRepository permissionRepository,
        IEventBus eventBus)
    {
        _profileRepository = profileRepository;
        _permissionRepository = permissionRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(SetProfilePermissionsCommand cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new AuthorizationNotFoundException($"Profile '{cmd.ProfileId}' not found.");

        if (profile.IsSystem)
            throw new AuthorizationForbiddenException($"Permissions on system profile '{profile.Name}' are managed automatically and cannot be overwritten via the API.");

        foreach (var permId in cmd.PermissionIds)
        {
            var permission = await _permissionRepository.FindByIdAsync(permId, ct);
            if (permission is null)
                throw new AuthorizationNotFoundException($"Permission '{permId}' not found.");
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
            await _eventBus.PublishAsync(new UserPermissionsChangedEvent(userId), ct);

        await _eventBus.PublishAsync(
            new ProfilePermissionsSetEvent(
                cmd.ProfileId,
                profile.Name,
                cmd.ActorUsername ?? AuthorizationActorNames.SystemActor),
            ct);
    }
}
