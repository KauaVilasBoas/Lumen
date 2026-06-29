using Lumen.Modules.Identity.Domain.Users;

namespace Lumen.Modules.Identity.Domain.Notifications;

internal interface IEmailConfirmationService
{
    Task SendConfirmationEmailAsync(User user, CancellationToken ct = default);
}
