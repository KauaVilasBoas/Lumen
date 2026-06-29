using FluentValidation;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using MediatR;

namespace Lumen.Modules.Identity.Application.Profiles.Create;

public sealed record CreateProfileCommand(string Name, string Description) : IRequest<CreateProfileResult>;

public sealed record CreateProfileResult(Guid Id, string Name, string Description);

internal sealed class CreateProfileCommandHandler
    : IRequestHandler<CreateProfileCommand, CreateProfileResult>
{
    public sealed class Validator : AbstractValidator<CreateProfileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Profile name is required.")
                .MaximumLength(128).WithMessage("Profile name must not exceed 128 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Profile description is required.")
                .MaximumLength(512).WithMessage("Profile description must not exceed 512 characters.");
        }
    }

    private readonly IProfileRepository _profileRepository;

    public CreateProfileCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<CreateProfileResult> Handle(CreateProfileCommand cmd, CancellationToken ct)
    {
        var nameAlreadyUsed = await _profileRepository.ActiveNameExistsAsync(cmd.Name, excludeId: null, ct);

        if (nameAlreadyUsed)
            throw new ConflictException($"A profile with name '{cmd.Name}' already exists.");

        var profile = Profile.Create(cmd.Name, cmd.Description);

        await _profileRepository.InsertAsync(profile, ct);

        return new CreateProfileResult(profile.Id, profile.Name, profile.Description);
    }
}
