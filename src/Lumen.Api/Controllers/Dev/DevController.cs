using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Infrastructure.Configuration;
using Lumen.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Lumen.Api.Controllers.Dev;

[Route("dev")]
[ApiExplorerSettings(GroupName = "Dev")]
[AllowAnonymous]
public sealed class DevController : ApiBaseController
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IOptions<IdentitySmtpOptions> _smtpOptions;
    private readonly IWebHostEnvironment _env;

    public DevController(
        IEmailService emailService,
        IEmailTemplateRenderer renderer,
        IOptions<IdentitySmtpOptions> smtpOptions,
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
            return BadRequest(new { error = DevDefaults.ToQueryParamRequired });

        var smtp = _smtpOptions.Value;

        var placeholders = new Dictionary<string, string>
        {
            [EmailPlaceholderKeys.UserName]        = DevDefaults.TestEmailRecipientDisplayName,
            [EmailPlaceholderKeys.ConfirmationUrl] = DevDefaults.TestEmailConfirmationUrl,
        };

        var (html, text) = _renderer.Render(EmailTemplateNames.EmailConfirmation, placeholders);

        var message = new EmailMessage(
            To: to,
            Subject: DevDefaults.TestEmailSubject,
            HtmlBody: html,
            TextBody: text);

        await _emailService.SendAsync(message, ct);

        return Ok(new
        {
            ok = true,
            to,
            smtp = $"{smtp.Host}:{smtp.Port}",
            viewer = DevDefaults.MailpitViewerUrl,
        });
    }
}
