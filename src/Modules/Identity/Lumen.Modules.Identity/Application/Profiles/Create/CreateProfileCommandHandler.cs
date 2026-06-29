using FluentValidation;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using MediatR;

namespace Lumen.Modules.Identity.Application.Profiles.Create;

internal sealed class CreateProfileCommandHandler
    : IRequestHandler<CreateProfileCommandHandler.Command, CreateProfileCommandHandler.Result>
{
    public sealed record Command(string Name, string Description) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
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
            throw new ConflictException($"A profile with name '{cmd.Name}' already exists.");

        var profile = Profile.Create(cmd.Name, cmd.Description);

        await _profileRepository.InsertAsync(profile, ct);

        return new Result(profile.Id, profile.Name, profile.Description);
    }
}
