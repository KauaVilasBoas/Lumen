using FluentValidation;
using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using MediatR;

namespace Lumen.Modules.Identity.Application.UserProfiles.Assign;

internal sealed class AssignUserProfileCommandHandler
    : IRequestHandler<AssignUserProfileCommandHandler.Command>
{
    public sealed record Command(Guid UserId, Guid ProfileId) : IRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.");

            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage("ProfileId is required.");
        }
    }

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IEventBus _eventBus;

    public AssignUserProfileCommandHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository,
        IEventBus eventBus)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
        _eventBus = eventBus;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException($"User '{cmd.UserId}' not found.");

        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new NotFoundException($"Profile '{cmd.ProfileId}' not found.");

        var existing = await _userProfileRepository.FindActiveAsync(cmd.UserId, cmd.ProfileId, ct);

        if (existing is not null)
            return;

        var userProfile = UserProfile.Create(cmd.UserId, cmd.ProfileId);
        await _userProfileRepository.InsertAsync(userProfile, ct);

        await _eventBus.PublishAsync(new UserPermissionsChangedEvent(cmd.UserId), ct);
        await _eventBus.PublishAsync(
            new UserProfileAssignedEvent(cmd.UserId, user.Username, cmd.ProfileId, profile.Name),
            ct);
    }
}
