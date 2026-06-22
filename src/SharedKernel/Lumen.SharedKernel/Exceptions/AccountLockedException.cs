namespace Lumen.SharedKernel.Exceptions;

public sealed class AccountLockedException : BusinessException
{
    public DateTime LockedUntil { get; }

    public AccountLockedException(DateTime lockedUntil)
        : base("Account is temporarily locked due to repeated failed login attempts.", 423)
    {
        LockedUntil = lockedUntil;
    }
}
