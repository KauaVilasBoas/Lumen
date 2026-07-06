using Lumen.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;

namespace Lumen.Identity.Application.Users;

internal static class UserStateResolver
{
    public static string Resolve(User user, DateTime asOfUtc)
    {
        if (user.IsDeleted)
            return UserStates.Deleted;

        if (IsLocked(user, asOfUtc))
            return UserStates.Locked;

        if (IsEmailPending(user))
            return UserStates.Pending;

        return UserStates.Active;
    }

    private static bool IsLocked(User user, DateTime asOfUtc)
        => user.LockedUntil.HasValue && user.LockedUntil.Value > asOfUtc;

    private static bool IsEmailPending(User user)
        => user.EmailConfirmedAt is null;
}
