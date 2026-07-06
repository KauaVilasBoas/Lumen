namespace Lumen.Identity.Domain.Notifications;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
