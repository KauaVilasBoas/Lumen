using MimeKit;

namespace Lumen.Modules.Identity.Infrastructure.Notifications;

internal interface ISmtpTransport
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}
