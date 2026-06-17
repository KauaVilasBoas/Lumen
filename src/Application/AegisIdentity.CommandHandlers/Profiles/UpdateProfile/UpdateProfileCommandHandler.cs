using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.Profiles.UpdateProfile;

public sealed class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommandHandler.Command>
{
    public sealed record Command(Guid Id, string Name, string Description) : IRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ProfileErrorMessages.ProfileIdRequired);

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ProfileErrorMessages.ProfileNameRequired)
                .MaximumLength(ValidationLimits.ProfileNameMaxLength)
                    .WithMessage(ProfileErrorMessages.ProfileNameTooLong);

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage(ProfileErrorMessages.ProfileDescriptionRequired)
                .MaximumLength(ValidationLimits.ProfileDescriptionMaxLength)
                    .WithMessage(ProfileErrorMessages.ProfileDescriptionTooLong);
        }
    }

    private readonly IProfileRepository _profileRepository;

    public UpdateProfileCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task Handle(Command cmd, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(string.Format(ProfileErrorMessages.ProfileNotFound, cmd.Id));

        if (profile.IsSystem && !string.Equals(profile.Name, cmd.Name, StringComparison.Ordinal))
            throw new ForbiddenException(string.Format(ProfileErrorMessages.SystemProfileCannotRename, profile.Name));

        var nameAlreadyUsed = await _profileRepository.ActiveNameExistsAsync(cmd.Name, excludeId: cmd.Id, ct);

        if (nameAlreadyUsed)
            throw new ConflictException(string.Format(ProfileErrorMessages.ProfileNameConflict, cmd.Name));

        profile.Update(cmd.Name, cmd.Description);

        await _profileRepository.UpdateAsync(profile, ct);
    }
}
