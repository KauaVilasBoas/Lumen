using AegisIdentity.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AegisIdentity.Api.Endpoints.Dev;

/// <summary>
/// Development-only endpoint that sends a smoke-test email through the local Mailpit relay.
/// Registered only when <c>ASPNETCORE_ENVIRONMENT=Development</c>.
/// </summary>
public static class EmailTestEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes
            .MapGet("/dev/email-test", HandleAsync)
            .WithName("DevEmailTest")
            .WithSummary("Dev: send a smoke-test email via Mailpit")
            .WithDescription(
                "Sends a test email through the configured local SMTP relay (Mailpit). " +
                "Available only in Development. " +
                "Open http://localhost:8025 after calling this endpoint to inspect the message.")
            .WithTags("Dev");
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string to,
        [FromServices] IOptions<SmtpOptions> smtpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return Results.BadRequest(new { error = "Query parameter 'to' is required." });
        }

        var smtp = smtpOptionsAccessor.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = "AegisIdentity Mailpit Smoke Test";
        message.Body = new TextPart("plain")
        {
            Text = $"""
                    AegisIdentity smoke test email.

                    Sent at : {DateTimeOffset.UtcNow:O}
                    SMTP    : {smtp.Host}:{smtp.Port}
                    From    : {smtp.From}
                    To      : {to}

                    If you can read this in Mailpit (http://localhost:8025), the local email relay is working.
                    """
        };

        try
        {
            using var client = new SmtpClient();

            // Mailpit does not require TLS; SecureSocketOptions.None skips STARTTLS negotiation.
            var socketOptions = smtp.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(smtp.User))
            {
                await client.AuthenticateAsync(smtp.User, smtp.Pass, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            return Results.Ok(new
            {
                ok = true,
                to,
                viewer = "http://localhost:8025"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to send smoke test email",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
