using AegisIdentity.Domain.Authorization;
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
                .NotEmpty().WithMessage("UserId is required.");

            RuleFor(x => x.ProfileId)
                .NotEmpty().WithMessage("ProfileId is required.");
        }
    }

    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;

    public RemoveUserProfileCommandHandler(
        IUserProfileRepository userProfileRepository,
        IPublisher publisher)
    {
        _userProfileRepository = userProfileRepository;
        _publisher = publisher;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var userProfile = await _userProfileRepository.FindActiveAsync(cmd.UserId, cmd.ProfileId, ct)
            ?? throw new NotFoundException($"Active assignment of user '{cmd.UserId}' to profile '{cmd.ProfileId}' not found.");

        userProfile.SoftDelete();

        await _userProfileRepository.UpdateAsync(userProfile, ct);

        await _publisher.Publish(new UserPermissionsChanged(cmd.UserId), ct);
    }
}
