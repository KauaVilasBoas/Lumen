using AegisIdentity.Domain.Audit;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.UserProfiles.RemoveUserProfile;

public sealed class RemoveUserProfileCommandHandler
    : IRequestHandler<RemoveUserProfileCommandHandler.Command>
{
    public sealed record Command(Guid UserId, Guid ProfileId) : IRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage(ProfileErrorMessages.UserIdRequired);

            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage(ProfileErrorMessages.ProfileIdRequired);
        }
    }

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;

    public RemoveUserProfileCommandHandler(
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
            ?? throw new NotFoundException(string.Format(ProfileErrorMessages.UserNotFoundForProfile, cmd.UserId));

        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new NotFoundException(string.Format(ProfileErrorMessages.ProfileNotFound, cmd.ProfileId));

        var userProfile = await _userProfileRepository.FindActiveAsync(cmd.UserId, cmd.ProfileId, ct)
            ?? throw new NotFoundException(string.Format(ProfileErrorMessages.ActiveAssignmentNotFound, cmd.UserId, cmd.ProfileId));

        userProfile.SoftDelete();

        await _userProfileRepository.UpdateAsync(userProfile, ct);

        await _publisher.Publish(new UserPermissionsChanged(cmd.UserId), ct);
        await _publisher.Publish(new UserProfileRemoved(cmd.UserId, user.Username, cmd.ProfileId, profile.Name), ct);
    }
}
