using System.Security.Cryptography;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Util;

namespace Lumen.Modules.Identity.Infrastructure.Notifications;

internal sealed class EmailConfirmationService : IEmailConfirmationService
{
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IAppSettings _appSettings;

    public EmailConfirmationService(
        IEmailConfirmationTokenRepository tokenRepository,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        IAppSettings appSettings)
    {
        _tokenRepository = tokenRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _appSettings = appSettings;
    }

    public async Task SendConfirmationEmailAsync(User user, CancellationToken ct = default)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);

        var confirmationToken = EmailConfirmationToken.Create(
            userId: user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.Add(TokenLifetimes.EmailConfirmation));

        await _tokenRepository.InsertAsync(confirmationToken, ct);

        var confirmationUrl =
            $"{_appSettings.BaseUrl}{EmailLinkPaths.ConfirmEmail}?token={Uri.EscapeDataString(rawToken)}";

        var placeholders = new Dictionary<string, string>
        {
            [EmailPlaceholderKeys.UserName]        = user.Username,
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
