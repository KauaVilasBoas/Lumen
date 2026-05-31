using AegisIdentity.Domain.Authorization;
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
                .NotEmpty().WithMessage("Profile id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Profile name is required.")
                .MaximumLength(128).WithMessage("Profile name must not exceed 128 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Profile description is required.")
                .MaximumLength(512).WithMessage("Profile description must not exceed 512 characters.");
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
            ?? throw new NotFoundException($"Profile '{cmd.Id}' not found.");

        var nameAlreadyUsed = await _profileRepository.ActiveNameExistsAsync(cmd.Name, excludeId: cmd.Id, ct);

        if (nameAlreadyUsed)
            throw new ConflictException($"A profile with name '{cmd.Name}' already exists.");

        profile.Update(cmd.Name, cmd.Description);

        await _profileRepository.UpdateAsync(profile, ct);
    }
}
