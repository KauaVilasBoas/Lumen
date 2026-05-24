using AegisIdentity.Domain.Notifications;

namespace AegisIdentity.Integration.Notifications;

// Adapts the concrete EmailTemplateRenderer to IEmailTemplateRenderer so that
// Application use cases can depend on the abstraction rather than the Integration type.
public sealed class EmailTemplateRendererAdapter : IEmailTemplateRenderer
{
    private readonly EmailTemplateRenderer _renderer;

    public EmailTemplateRendererAdapter(EmailTemplateRenderer renderer)
    {
        _renderer = renderer;
    }

    public (string HtmlBody, string TextBody) Render(
        string templateName,
        IReadOnlyDictionary<string, string> placeholders)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

        if (!Enum.TryParse<EmailTemplate>(templateName, ignoreCase: true, out var template))
            throw new ArgumentException(
                $"No email template named '{templateName}' is registered.", nameof(templateName));

        return _renderer.Render(template, placeholders);
    }
}
