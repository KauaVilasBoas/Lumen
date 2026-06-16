using AegisIdentity.Api.Controllers;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Infrastructure.Configuration;
using AegisIdentity.Integration.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Api.Controllers.Dev;

[ApiController]
[Route("dev")]
[ApiExplorerSettings(GroupName = "Dev")]
[AllowAnonymous]
public sealed class DevController : ApiBaseController
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

        await _emailService.SendAsync(message, ct);

        return Ok(new
        {
            ok = true,
            to,
            smtp = $"{smtp.Host}:{smtp.Port}",
            viewer = "http://localhost:8025",
        });
    }
}
