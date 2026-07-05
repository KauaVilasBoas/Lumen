using FluentValidation;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Modularity;
using MediatR;

namespace Lumen.Authorization.Application.UserProfiles.Assign;

public sealed record AssignUserProfileCommand(Guid UserId, Guid ProfileId) : IRequest;

internal sealed class AssignUserProfileCommandHandler
    : IRequestHandler<AssignUserProfileCommand>
{
    public sealed class Validator : AbstractValidator<AssignUserProfileCommand>
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

    public AssignUserProfileCommandHandler(
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

    public async Task Handle(AssignUserProfileCommand cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new AuthorizationNotFoundException($"Profile '{cmd.ProfileId}' not found.");

        var existing = await _userProfileRepository.FindActiveAsync(cmd.UserId, cmd.ProfileId, ct);

        if (existing is not null)
            return;

        var userProfile = UserProfile.Create(cmd.UserId, cmd.ProfileId);
        await _userProfileRepository.InsertAsync(userProfile, ct);

        var username = await _userDirectory.GetDisplayNameAsync(cmd.UserId, ct) ?? string.Empty;

        await _eventBus.PublishAsync(new UserPermissionsChangedEvent(cmd.UserId), ct);
        await _eventBus.PublishAsync(
            new UserProfileAssignedEvent(cmd.UserId, username, cmd.ProfileId, profile.Name),
            ct);
    }
}
