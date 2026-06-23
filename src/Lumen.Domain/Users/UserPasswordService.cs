using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.SharedKernel.Constants;

namespace Lumen.Domain.Users;

public sealed class UserPasswordService : IUserPasswordService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;

    public UserPasswordService(
        IRefreshTokenRepository refreshTokenRepository,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
    }

    public async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await _refreshTokenRepository.FindByUserIdAsync(userId, ct);

        foreach (var token in tokens.Where(t => t.IsActive()))
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token, ct);
        }
    }

    public async Task SendPasswordChangedEmailAsync(User user, CancellationToken ct = default)
    {
        var placeholders = new Dictionary<string, string>
        {
            [EmailPlaceholderKeys.UserName] = user.Username,
        };

        var (htmlBody, textBody) = _templateRenderer.Render(EmailTemplateNames.PasswordChanged, placeholders);

        var message = new EmailMessage(
            To: user.Email,
            Subject: EmailSubjects.PasswordChanged,
            HtmlBody: htmlBody,
            TextBody: textBody);

        await _emailService.SendAsync(message, ct);
    }
}
