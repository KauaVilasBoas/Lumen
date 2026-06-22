using Lumen.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Lumen.Integration.Notifications;

public sealed class MailKitSmtpTransport : ISmtpTransport
{
    // 10s connect timeout per the EMAIL-01 card risk mitigation.
    internal static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    private readonly SmtpOptions _options;

    public MailKitSmtpTransport(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        using var client = new SmtpClient
        {
            // MailKit honours this for ConnectAsync; AuthenticateAsync and SendAsync
            // additionally observe the CancellationToken passed below.
            Timeout = (int)ConnectTimeout.TotalMilliseconds,
        };

        // Mailpit accepts plain SMTP; StartTlsWhenAvailable lets a single config
        // value work for both Mailpit (dev) and real providers (prod).
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
