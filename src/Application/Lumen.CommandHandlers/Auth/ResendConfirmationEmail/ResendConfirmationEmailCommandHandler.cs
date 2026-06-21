using System.Security.Cryptography;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Util;
using FluentValidation;
using MediatR;

namespace AegisIdentity.CommandHandlers.Auth.ResendConfirmationEmail;

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

    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IAppSettings _appSettings;

    public ResendConfirmationEmailCommandHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IAppSettings appSettings)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _appSettings = appSettings;
    }

    public async Task<Unit> Handle(Command cmd, CancellationToken ct)
    {
        var normalizedEmail = User.NormalizeEmail(cmd.Email);
        var user = await _userRepository.FindByEmailAsync(normalizedEmail, ct);

        if (user is null || user.IsActive)
            return Unit.Value;

        await _tokenRepository.InvalidateByUserIdAsync(user.Id, ct);
        await SendConfirmationEmailAsync(user, ct);

        return Unit.Value;
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
            $"{_appSettings.BaseUrl}{EmailLinkPaths.ConfirmEmail}?token={Uri.EscapeDataString(rawToken)}";

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
