using MimeKit;

namespace Lumen.Integration.Notifications;

// Seam between MailKitEmailService and the MailKit SmtpClient so the service
// can be unit-tested without standing up a real SMTP server. Production binds
// this to MailKitSmtpTransport; tests provide a fake.
public interface ISmtpTransport
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}
