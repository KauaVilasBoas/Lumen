using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.Profiles.CreateProfile;

public sealed class CreateProfileCommandHandler
    : IRequestHandler<CreateProfileCommandHandler.Command, CreateProfileCommandHandler.Result>
{
    public sealed record Command(string Name, string Description) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
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

    public sealed record Result(Guid Id, string Name, string Description);

    private readonly IProfileRepository _profileRepository;

    public CreateProfileCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result> Handle(Command cmd, CancellationToken ct)
    {
        var nameAlreadyUsed = await _profileRepository.ActiveNameExistsAsync(cmd.Name, excludeId: null, ct);

        if (nameAlreadyUsed)
            throw new ConflictException(string.Format(ProfileErrorMessages.ProfileNameConflict, cmd.Name));

        var profile = Domain.Authorization.Profile.Create(cmd.Name, cmd.Description);

        await _profileRepository.InsertAsync(profile, ct);

        return new Result(profile.Id, profile.Name, profile.Description);
    }
}
