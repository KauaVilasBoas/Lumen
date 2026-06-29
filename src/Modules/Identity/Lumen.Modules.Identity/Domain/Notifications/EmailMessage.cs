namespace Lumen.Modules.Identity.Domain.Notifications;

internal sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);
