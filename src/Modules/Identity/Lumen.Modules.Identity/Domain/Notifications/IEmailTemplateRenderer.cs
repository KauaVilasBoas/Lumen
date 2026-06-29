namespace Lumen.Modules.Identity.Domain.Notifications;

internal interface IEmailTemplateRenderer
{
    (string HtmlBody, string TextBody) Render(string templateName, IReadOnlyDictionary<string, string> placeholders);
}
