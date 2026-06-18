using System.Security.Cryptography;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Util;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommandHandler.Command, Unit>
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

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IAppSettings appSettings,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<Unit> Handle(Command cmd, CancellationToken ct)
    {
        var normalizedEmail = User.NormalizeEmail(cmd.Email);
        var user = await _userRepository.FindByEmailAsync(normalizedEmail, ct);

        if (user is null)
        {
            await PerformDummyWorkToMitigateTiming(ct);
            return Unit.Value;
        }

        _logger.LogInformation(
            "Password reset requested for UserId {UserId}",
            user.Id);

        await SendPasswordResetEmailAsync(user, ct);

        return Unit.Value;
    }

    private async Task SendPasswordResetEmailAsync(User user, CancellationToken ct)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);

        var resetToken = PasswordResetToken.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.Add(TokenLifetime));

        await _tokenRepository.InsertAsync(resetToken, ct);

        var resetUrl =
            $"{_appSettings.BaseUrl}{EmailLinkPaths.ResetPassword}?token={Uri.EscapeDataString(rawToken)}";

        var placeholders = new Dictionary<string, string>
        {
            [EmailPlaceholderKeys.UserName] = user.Username,
            [EmailPlaceholderKeys.ResetUrl] = resetUrl,
        };

        var (htmlBody, textBody) = _templateRenderer.Render(EmailTemplateNames.PasswordReset, placeholders);

        var message = new EmailMessage(
            To: user.Email,
            Subject: EmailSubjects.PasswordReset,
            HtmlBody: htmlBody,
            TextBody: textBody);

        await _emailService.SendAsync(message, ct);
    }

    private static async Task PerformDummyWorkToMitigateTiming(CancellationToken ct)
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizes.RawTokenBytes);
        _ = Sha256Hasher.ComputeHex(Base64UrlEncoder.Encode(bytes));
        await Task.Delay(TimeSpan.FromMilliseconds(50), ct).ConfigureAwait(false);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizes.RawTokenBytes);
        return Base64UrlEncoder.Encode(bytes);
    }
}
