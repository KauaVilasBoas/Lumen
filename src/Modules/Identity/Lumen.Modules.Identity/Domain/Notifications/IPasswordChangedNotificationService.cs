using Lumen.Modules.Identity.Domain.Users;

namespace Lumen.Modules.Identity.Domain.Notifications;

internal interface IPasswordChangedNotificationService
{
    Task SendPasswordChangedEmailAsync(User user, CancellationToken ct = default);
}
