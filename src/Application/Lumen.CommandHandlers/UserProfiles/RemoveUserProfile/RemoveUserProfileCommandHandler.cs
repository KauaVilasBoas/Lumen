using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace Lumen.CommandHandlers.UserProfiles.RemoveUserProfile;

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

    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public RemoveUserProfileCommandHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IUserProfileRepository userProfileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException($"User '{cmd.UserId}' not found.");

        var profile = await _profileRepository.FindByIdAsync(cmd.ProfileId, ct)
            ?? throw new NotFoundException($"Profile '{cmd.ProfileId}' not found.");

        var userProfile = await _userProfileRepository.FindActiveAsync(cmd.UserId, cmd.ProfileId, ct)
            ?? throw new NotFoundException($"Active assignment of user '{cmd.UserId}' to profile '{cmd.ProfileId}' not found.");

        user.RemoveProfile(userProfile, profile);

        await _userProfileRepository.UpdateAsync(userProfile, ct);
    }
}
