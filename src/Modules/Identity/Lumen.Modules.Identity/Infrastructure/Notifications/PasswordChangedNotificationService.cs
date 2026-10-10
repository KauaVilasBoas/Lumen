using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;

namespace Lumen.Modules.Identity.Infrastructure.Notifications;

internal sealed class PasswordChangedNotificationService : IPasswordChangedNotificationService
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;

    public PasswordChangedNotificationService(
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer)
    {
        _emailService = emailService;
        _templateRenderer = templateRenderer;
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
