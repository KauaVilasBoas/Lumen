using MimeKit;

namespace Lumen.Identity.Infrastructure.Notifications;

internal interface ISmtpTransport
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}
