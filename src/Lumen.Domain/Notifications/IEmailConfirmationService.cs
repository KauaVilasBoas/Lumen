using Lumen.Domain.Users;

namespace Lumen.Domain.Notifications;

public interface IEmailConfirmationService
{
    Task SendConfirmationEmailAsync(User user, CancellationToken ct = default);
}
