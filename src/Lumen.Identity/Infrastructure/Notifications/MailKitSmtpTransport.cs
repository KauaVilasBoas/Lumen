using Lumen.Identity.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Lumen.Identity.Infrastructure.Notifications;

internal sealed class MailKitSmtpTransport : ISmtpTransport
{
    internal static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    private readonly IdentitySmtpOptions _options;

    public MailKitSmtpTransport(IOptions<IdentitySmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient
        {
            Timeout = (int)ConnectTimeout.TotalMilliseconds,
        };

        var socketOptions = _options.UseStartTls
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, ct);

        if (!string.IsNullOrWhiteSpace(_options.User))
            await client.AuthenticateAsync(_options.User, _options.Pass, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
