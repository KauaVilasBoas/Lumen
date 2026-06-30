using FluentValidation;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Auth.Register;

public sealed record RegisterCommand(string Email, string Username, string Password) : IRequest<RegisterResult>;

public sealed record RegisterResult(string Id, string Email, string Username);

internal sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public sealed class Validator : AbstractValidator<RegisterCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(AuthErrorMessages.EmailRequired)
                .EmailAddress().WithMessage(AuthErrorMessages.EmailInvalid)
                .MaximumLength(ValidationLimits.EmailMaxLength).WithMessage(AuthErrorMessages.EmailTooLong);

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage(AuthErrorMessages.UsernameRequired)
                .MinimumLength(ValidationLimits.UsernameMinLength)
                    .WithMessage(string.Format(AuthErrorMessages.UsernameTooShort, ValidationLimits.UsernameMinLength))
                .MaximumLength(ValidationLimits.UsernameMaxLength)
                    .WithMessage(string.Format(AuthErrorMessages.UsernameTooLong, ValidationLimits.UsernameMaxLength))
                .Matches(ValidationLimits.UsernameAllowedCharsPattern)
                    .WithMessage(AuthErrorMessages.UsernameInvalidChars);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(AuthErrorMessages.PasswordRequired);
        }
    }

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IEmailConfirmationService emailConfirmationService,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<RegisterResult> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        var passwordValidation = await _passwordValidator.ValidatePasswordAsync(
            new(cmd.Password, cmd.Email, cmd.Username), ct);

        if (!passwordValidation.IsValid)
            throw new SharedKernel.Exceptions.ValidationException("password", passwordValidation.Errors);

        var passwordHash = _passwordHasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, cmd.Username, passwordHash);

        try
        {
            await _userRepository.InsertAsync(user, ct);
        }
        catch (DuplicateEmailException)
        {
            throw new ConflictException(AuthErrorMessages.EmailAlreadyInUse);
        }
        catch (DuplicateUsernameException)
        {
            throw new ConflictException(AuthErrorMessages.UsernameAlreadyInUse);
        }

        _logger.LogInformation("User {UserId} registered with email {Email}", user.Id, user.Email);

        await _emailConfirmationService.SendConfirmationEmailAsync(user, ct);

        return new RegisterResult(user.Id.ToString(), user.Email, user.Username);
    }
}
