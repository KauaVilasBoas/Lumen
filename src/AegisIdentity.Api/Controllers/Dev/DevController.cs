using AegisIdentity.Domain.Notifications;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Integration.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Api.Controllers.Dev;

/// <summary>
/// Development-only controller that exercises internal infrastructure pipelines.
/// Each action verifies the environment at runtime and returns 404 in non-Development
/// environments, mirroring the fail-safe behaviour of the original Minimal Endpoint.
/// </summary>
[ApiController]
[Route("dev")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Dev")]
public sealed class DevController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly EmailTemplateRenderer _renderer;
    private readonly IOptions<SmtpOptions> _smtpOptions;
    private readonly IWebHostEnvironment _env;

    public DevController(
        IEmailService emailService,
        EmailTemplateRenderer renderer,
        IOptions<SmtpOptions> smtpOptions,
        IWebHostEnvironment env)
    {
        _emailService = emailService;
        _renderer = renderer;
        _smtpOptions = smtpOptions;
        _env = env;
    }

    /// <summary>
    /// Dev: send a smoke-test email via Mailpit.
    /// </summary>
    /// <remarks>
    /// Renders the EmailConfirmation template and dispatches it through IEmailService.
    /// Available only in Development.
    /// Open http://localhost:8025 after calling this endpoint to inspect the message.
    /// </remarks>
    [HttpGet("email-test")]
    public async Task<IActionResult> EmailTest(
        [FromQuery] string to,
        CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(new { error = "Query parameter 'to' is required." });

        var smtp = _smtpOptions.Value;

        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = "Developer",
            ["ConfirmationUrl"] = "http://localhost:5237/dev/email-test",
        };

        var (html, text) = _renderer.Render(EmailTemplate.EmailConfirmation, placeholders);

        var message = new EmailMessage(
            To: to,
            Subject: "AegisIdentity Mailpit Smoke Test",
            HtmlBody: html,
            TextBody: text);

        // IEmailService is fail-open: transport errors are logged and swallowed.
        // The endpoint always reports 200 — open Mailpit to confirm delivery.
        await _emailService.SendAsync(message, ct);

        return Ok(new
        {
            ok = true,
            to,
            smtp = $"{smtp.Host}:{smtp.Port}",
            viewer = "http://localhost:8025",
            note = "IEmailService is fail-open. Open Mailpit to verify the message actually arrived.",
        });
    }
}
