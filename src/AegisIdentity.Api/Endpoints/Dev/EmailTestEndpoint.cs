using AegisIdentity.Domain.Notifications;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Infrastructure.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Api.Endpoints.Dev;

// Development-only endpoint that exercises the full IEmailService pipeline
// (rendering + multipart MIME + SMTP) against the local Mailpit relay.
// Registered only when ASPNETCORE_ENVIRONMENT=Development.
public static class EmailTestEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes
            .MapGet("/dev/email-test", HandleAsync)
            .WithName("DevEmailTest")
            .WithSummary("Dev: send a smoke-test email via Mailpit")
            .WithDescription(
                "Renders the EmailConfirmation template and dispatches it through IEmailService. " +
                "Available only in Development. " +
                "Open http://localhost:8025 after calling this endpoint to inspect the message.")
            .WithTags("Dev");
    }

    private static async Task<IResult> HandleAsync(
        [FromQuery] string to,
        [FromServices] IEmailService emailService,
        [FromServices] EmailTemplateRenderer renderer,
        [FromServices] IOptions<SmtpOptions> smtpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
            return Results.BadRequest(new { error = "Query parameter 'to' is required." });

        var smtp = smtpOptionsAccessor.Value;

        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = "Developer",
            ["ConfirmationUrl"] = "http://localhost:5237/dev/email-test",
        };

        var (html, text) = renderer.Render(EmailTemplate.EmailConfirmation, placeholders);

        var message = new EmailMessage(
            To: to,
            Subject: "AegisIdentity Mailpit Smoke Test",
            HtmlBody: html,
            TextBody: text);

        // IEmailService is fail-open: returns successfully even if SMTP fails (logged Warning).
        // The endpoint always reports 200 — open Mailpit to confirm delivery.
        await emailService.SendAsync(message, cancellationToken);

        return Results.Ok(new
        {
            ok = true,
            to,
            smtp = $"{smtp.Host}:{smtp.Port}",
            viewer = "http://localhost:8025",
            note = "IEmailService is fail-open. Open Mailpit to verify the message actually arrived.",
        });
    }
}
