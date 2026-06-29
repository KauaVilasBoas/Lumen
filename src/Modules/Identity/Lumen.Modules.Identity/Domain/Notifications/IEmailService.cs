namespace Lumen.Modules.Identity.Domain.Notifications;

internal interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
