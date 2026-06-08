using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.UserProfiles.AssignUserProfile;

public sealed class AssignUserProfileCommandHandler
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
    private readonly IPublisher _publisher;

    public AssignUserProfileCommandHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository,
        IPublisher publisher)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
        _publisher = publisher;
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

        await _publisher.Publish(new UserPermissionsChanged(cmd.UserId), ct);
        await _publisher.Publish(new UserProfileAssigned(cmd.UserId, user.Username, cmd.ProfileId, profile.Name), ct);
    }
}
