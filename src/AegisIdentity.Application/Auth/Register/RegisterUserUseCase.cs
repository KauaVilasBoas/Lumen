using System.Security.Cryptography;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Util;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Application.Auth.Register;

public sealed class RegisterUserUseCase : IRegisterUserUseCase
{
    // Token is valid for 24 hours — long enough to be user-friendly, short
    // enough to limit the exposure window of a compromised confirmation link.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<RegisterUserUseCase> _logger;

    public RegisterUserUseCase(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IAppSettings appSettings,
        ILogger<RegisterUserUseCase> logger)
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

    public async Task<RegisterResult> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var passwordValidation = await _passwordValidator.ValidatePasswordAsync(
            new(request.Password, request.Email, request.Username), ct);

        if (!passwordValidation.IsValid)
            return new RegisterResult.WeakPassword(passwordValidation.Errors);

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(request.Email, request.Username, passwordHash);

        try
        {
            await _userRepository.InsertAsync(user, ct);
        }
        catch (DuplicateEmailException)
        {
            return new RegisterResult.DuplicateEmail();
        }
        catch (DuplicateUsernameException)
        {
            return new RegisterResult.DuplicateUsername();
        }

        _logger.LogInformation(
            "User {UserId} registered with email {Email}", user.Id, user.Email);

        await SendConfirmationEmailAsync(user, ct);

        return new RegisterResult.Success(new RegisterResponse(user.Id, user.Email, user.Username));
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

        // IEmailService is fail-open: transport errors are logged and swallowed.
        // The registration is already committed — a confirmation email failure
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
