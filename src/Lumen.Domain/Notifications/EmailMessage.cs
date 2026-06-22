namespace Lumen.Domain.Notifications;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);
