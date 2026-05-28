using System.Security.Cryptography;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using AegisIdentity.SharedKernel.Util;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Auth.Register;

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
                .Matches("^[a-zA-Z0-9_-]+$")
                    .WithMessage(AuthErrorMessages.UsernameInvalidChars);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(AuthErrorMessages.PasswordRequired);
        }
    }

    public sealed record Result(string Id, string Email, string Username);

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IAppSettings appSettings,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _appSettings = appSettings;
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

        await SendConfirmationEmailAsync(user, ct);

        return new Result(user.Id, user.Email, user.Username);
    }

    private async Task SendConfirmationEmailAsync(User user, CancellationToken ct)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);

        var confirmationToken = EmailConfirmationToken.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.Add(TokenLifetime));

        await _tokenRepository.InsertAsync(confirmationToken, ct);

        var confirmationUrl =
            $"{_appSettings.BaseUrl}/api/auth/confirm-email?token={Uri.EscapeDataString(rawToken)}";

        var placeholders = new Dictionary<string, string>
        {
            [EmailPlaceholderKeys.UserName] = user.Username,
            [EmailPlaceholderKeys.ConfirmationUrl] = confirmationUrl,
        };

        var (htmlBody, textBody) = _templateRenderer.Render(EmailTemplateNames.EmailConfirmation, placeholders);

        var message = new EmailMessage(
            To: user.Email,
            Subject: EmailSubjects.EmailConfirmation,
            HtmlBody: htmlBody,
            TextBody: textBody);

        await _emailService.SendAsync(message, ct);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizes.RawTokenBytes);
        return Base64UrlEncoder.Encode(bytes);
    }
}
