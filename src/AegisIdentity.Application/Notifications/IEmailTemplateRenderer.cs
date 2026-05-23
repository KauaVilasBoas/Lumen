namespace AegisIdentity.Application.Notifications;

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
