using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using FluentValidation;
using MediatR;

namespace Lumen.CommandHandlers.Auth.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandHandler
    : IRequestHandler<ResendConfirmationEmailCommandHandler.Command, Unit>
{
    public sealed record Command(string Email) : IRequest<Unit>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(AuthErrorMessages.EmailRequired)
                .EmailAddress().WithMessage(AuthErrorMessages.EmailInvalid)
                .MaximumLength(ValidationLimits.EmailMaxLength).WithMessage(AuthErrorMessages.EmailTooLong);
        }
    }

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IEmailConfirmationService _emailConfirmationService;

    public ResendConfirmationEmailCommandHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IEmailConfirmationService emailConfirmationService)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailConfirmationService = emailConfirmationService;
    }

    public async Task<Unit> Handle(Command cmd, CancellationToken ct)
    {
        var normalizedEmail = User.NormalizeEmail(cmd.Email);
        var user = await _userRepository.FindByEmailAsync(normalizedEmail, ct);

        if (user is null || user.IsActive)
            return Unit.Value;

        await _tokenRepository.InvalidateByUserIdAsync(user.Id, ct);
        await _emailConfirmationService.SendConfirmationEmailAsync(user, ct);

        return Unit.Value;
    }
}
