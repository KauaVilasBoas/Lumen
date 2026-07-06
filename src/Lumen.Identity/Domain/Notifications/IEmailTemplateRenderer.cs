namespace Lumen.Identity.Domain.Notifications;

public interface IEmailTemplateRenderer
{
    (string HtmlBody, string TextBody) Render(string templateName, IReadOnlyDictionary<string, string> placeholders);
}
