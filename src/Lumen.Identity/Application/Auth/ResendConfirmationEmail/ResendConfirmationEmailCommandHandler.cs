using FluentValidation;
using Lumen.Identity.Domain.Notifications;
using Lumen.Identity.Domain.Tokens;
using Lumen.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using MediatR;

namespace Lumen.Identity.Application.Auth.ResendConfirmationEmail;

public sealed record ResendConfirmationEmailCommand(string Email) : IRequest<Unit>;

internal sealed class ResendConfirmationEmailCommandHandler
    : IRequestHandler<ResendConfirmationEmailCommand, Unit>
{
    public sealed class Validator : AbstractValidator<ResendConfirmationEmailCommand>
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

    public async Task<Unit> Handle(ResendConfirmationEmailCommand cmd, CancellationToken ct)
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
