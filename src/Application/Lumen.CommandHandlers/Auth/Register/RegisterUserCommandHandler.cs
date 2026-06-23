using Lumen.Domain.Configuration;
using Lumen.Domain.Notifications;
using Lumen.Domain.Security;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.CommandHandlers.Auth.Register;

public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommandHandler.Command, RegisterUserCommandHandler.Result>
{
    public sealed record Command(string Email, string Username, string Password) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
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

    public sealed record Result(string Id, string Email, string Username);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IEmailConfirmationService emailConfirmationService,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<Result> Handle(Command cmd, CancellationToken ct)
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

        return new Result(user.Id.ToString(), user.Email, user.Username);
    }
}
