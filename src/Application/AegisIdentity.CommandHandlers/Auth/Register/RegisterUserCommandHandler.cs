using System.Security.Cryptography;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Util;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Auth.Register;

/// <summary>
/// Handles user registration: validates the password, creates the user, persists an
/// email-confirmation token, and dispatches the confirmation email (fail-open).
/// </summary>
public sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommandHandler.Command, RegisterUserCommandHandler.Result>
{
    // ── Nested types ─────────────────────────────────────────────────────────

    /// <summary>Registration input. Validated at the API boundary before dispatch.</summary>
    public sealed record Command(string Email, string Username, string Password) : IRequest<Result>;

    /// <summary>
    /// Structural (non-I/O) validator for the registration command.
    /// Executed by <see cref="Behaviors.ValidationBehavior{TRequest,TResponse}"/> before Handle.
    /// Rules that require I/O (uniqueness check, HIBP) remain inside Handle().
    /// </summary>
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

    /// <summary>Discriminated union result — one subtype per outcome.</summary>
    public abstract class Result
    {
        private Result() { }

        public sealed class Success : Result
        {
            public string Id { get; }
            public string Email { get; }
            public string Username { get; }

            public Success(string id, string email, string username)
            {
                Id = id;
                Email = email;
                Username = username;
            }
        }

        public sealed class WeakPassword : Result
        {
            public IReadOnlyList<string> Errors { get; }

            public WeakPassword(IReadOnlyList<string> errors) => Errors = errors;
        }

        public sealed class DuplicateEmail : Result { }

        public sealed class DuplicateUsername : Result { }
    }

    // ── Constants ─────────────────────────────────────────────────────────────

    // Token is valid for 24 hours — long enough to be user-friendly, short
    // enough to limit the exposure window of a compromised confirmation link.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    // ── Dependencies ──────────────────────────────────────────────────────────

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

    // ── Handler ───────────────────────────────────────────────────────────────

    public async Task<Result> Handle(Command cmd, CancellationToken ct)
    {
        var passwordValidation = await _passwordValidator.ValidatePasswordAsync(
            new(cmd.Password, cmd.Email, cmd.Username), ct);

        if (!passwordValidation.IsValid)
            return new Result.WeakPassword(passwordValidation.Errors);

        var passwordHash = _passwordHasher.Hash(cmd.Password);
        var user = User.Create(cmd.Email, cmd.Username, passwordHash);

        try
        {
            await _userRepository.InsertAsync(user, ct);
        }
        catch (DuplicateEmailException)
        {
            return new Result.DuplicateEmail();
        }
        catch (DuplicateUsernameException)
        {
            return new Result.DuplicateUsername();
        }

        _logger.LogInformation(
            "User {UserId} registered with email {Email}", user.Id, user.Email);

        await SendConfirmationEmailAsync(user, ct);

        return new Result.Success(user.Id, user.Email, user.Username);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

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

        // IEmailService is fail-open: transport errors are logged and swallowed.
        // The registration is already committed — a confirmation-email failure
        // must not roll it back.
        await _emailService.SendAsync(message, ct);
    }

    private static string GenerateRawToken()
    {
        // 32 bytes of entropy ≈ 256-bit token — no practical brute-force risk
        // for a single-use, expiring value stored hashed in the database.
        var bytes = RandomNumberGenerator.GetBytes(TokenSizes.RawTokenBytes);
        return Base64UrlEncoder.Encode(bytes);
    }
}
