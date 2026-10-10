using Lumen.SharedKernel.Constants;

namespace Lumen.Modules.Identity.Domain.Tokens;

internal static class OneTimeTokenPolicy
{
    internal static void ValidateCreation(string tokenHash, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException(AuthErrorMessages.ExpiresAtMustBeInFuture, nameof(expiresAt));
    }

    internal static bool IsExpired(DateTime expiresAt) => DateTime.UtcNow >= expiresAt;

    internal static bool IsUsed(DateTime? usedAt) => usedAt.HasValue;

    internal static bool IsValid(DateTime expiresAt, DateTime? usedAt) =>
        !IsExpired(expiresAt) && !IsUsed(usedAt);
}
