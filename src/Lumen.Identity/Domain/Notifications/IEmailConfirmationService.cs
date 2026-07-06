using Lumen.Identity.Domain.Users;

namespace Lumen.Identity.Domain.Notifications;

public interface IEmailConfirmationService
{
    Task SendConfirmationEmailAsync(User user, CancellationToken ct = default);
}
