using AegisIdentity.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AegisIdentity.Api.Endpoints.Dev;

// ─── DEV-ONLY ─────────────────────────────────────────────────────────────────
// This endpoint is registered ONLY when ASPNETCORE_ENVIRONMENT=Development.
// It is never available in Staging or Production — see Program.cs registration.
//
// Purpose: smoke test the local Mailpit relay without writing a full use case.
// Avoids polluting the domain layer with test-only logic.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Development-only endpoint for verifying the local Mailpit SMTP relay.
/// Sends a test email and returns a JSON response with the Mailpit viewer URL.
/// </summary>
public static class EmailTestEndpoint
{
    /// <summary>
    /// Registers <c>GET /dev/email-test</c> on the provided <see cref="IEndpointRouteBuilder"/>.
    /// Must be called only when <c>app.Environment.IsDevelopment()</c> is true.
    /// </summary>
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

            // Mailpit does not require TLS; SecureSocketOptions.None avoids STARTTLS negotiation.
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
