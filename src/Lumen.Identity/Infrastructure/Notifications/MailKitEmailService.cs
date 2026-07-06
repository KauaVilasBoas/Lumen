using Lumen.Identity.Domain.Notifications;
using Lumen.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Lumen.Identity.Infrastructure.Notifications;

internal sealed class MailKitEmailService : IEmailService
{
    internal const int MaxAttempts = 2;
    internal static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly ISmtpTransport _transport;
    private readonly IdentitySmtpOptions _options;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(
        ISmtpTransport transport,
        IOptions<IdentitySmtpOptions> options,
        ILogger<MailKitEmailService> logger)
    {
        _transport = transport;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mime = BuildMime(message);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await _transport.SendAsync(mime, ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt < MaxAttempts)
                {
                    _logger.LogWarning(
                        ex,
                        "Email send to {Recipient} (subject {Subject}) failed on attempt {Attempt}/{Max}; retrying.",
                        message.To, message.Subject, attempt, MaxAttempts);

                    try
                    {
                        await Task.Delay(RetryDelay, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }

                    continue;
                }

                _logger.LogWarning(
                    ex,
                    "Email send to {Recipient} (subject {Subject}) failed after {Max} attempts; giving up.",
                    message.To, message.Subject, MaxAttempts);
                return;
            }
        }
    }

    private MimeMessage BuildMime(EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(_options.From));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        };

        mime.Body = builder.ToMessageBody();

        if (string.IsNullOrEmpty(message.HtmlBody))
            mime.Body = new TextPart(TextFormat.Plain) { Text = message.TextBody };
        else if (string.IsNullOrEmpty(message.TextBody))
            mime.Body = new TextPart(TextFormat.Html) { Text = message.HtmlBody };

        return mime;
    }
}
