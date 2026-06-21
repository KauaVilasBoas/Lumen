namespace AegisIdentity.Domain.Notifications;

public interface IEmailService
{
    // Implementations must not propagate transport errors — they log and swallow.
    // Callers can therefore await the call without try/catch for "best effort" semantics.
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
