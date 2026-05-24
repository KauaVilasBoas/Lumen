namespace AegisIdentity.Domain.Notifications;

/// <summary>
/// Port for rendering transactional email templates.
/// Defined in Domain so command handlers can request rendered email bodies
/// without depending on the concrete template engine in Infrastructure.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders a named email template by substituting the given placeholders.
    /// Returns the rendered HTML and plain-text bodies.
    /// </summary>
    (string HtmlBody, string TextBody) Render(
        string templateName,
        IReadOnlyDictionary<string, string> placeholders);
}
