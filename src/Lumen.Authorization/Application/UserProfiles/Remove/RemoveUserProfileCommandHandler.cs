using FluentValidation;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Modularity;
using MediatR;

namespace Lumen.Authorization.Application.UserProfiles.Remove;

/// <param name="UserId">The user whose assignment will be removed.</param>
/// <param name="ProfileId">The profile assignment to remove.</param>
/// <param name="ScopeId">The tenant scope of the assignment, or <c>null</c> for a global assignment.</param>
public sealed record RemoveUserProfileCommand(Guid UserId, Guid ProfileId, Guid? ScopeId = null) : IRequest;

internal sealed class RemoveUserProfileCommandHandler
    : IRequestHandler<RemoveUserProfileCommand>
{
    public sealed class Validator : AbstractValidator<RemoveUserProfileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");

            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage("ProfileId is required.");
        }
    }

    private readonly IUserDirectory _userDirectory;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IEventBus _eventBus;

    public RemoveUserProfileCommandHandler(
        IUserDirectory userDirectory,
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository,
        IEventBus eventBus)
    {
        _userDirectory = userDirectory;
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(RemoveUserProfileCommand cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new AuthorizationNotFoundException($"Profile '{cmd.ProfileId}' not found.");

        var userProfile = await _userProfileRepository.FindActiveAsync(cmd.UserId, cmd.ProfileId, cmd.ScopeId, ct)
            ?? throw new AuthorizationNotFoundException($"Active assignment of user '{cmd.UserId}' to profile '{cmd.ProfileId}' not found.");

        userProfile.SoftDelete();

        await _userProfileRepository.UpdateAsync(userProfile, ct);

        var username = await _userDirectory.GetDisplayNameAsync(cmd.UserId, ct) ?? string.Empty;

        await _eventBus.PublishAsync(new UserPermissionsChangedEvent(cmd.UserId, cmd.ScopeId), ct);
        await _eventBus.PublishAsync(
            new UserProfileRemovedEvent(cmd.UserId, username, cmd.ProfileId, profile.Name),
            ct);
    }
}
