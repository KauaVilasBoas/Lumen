namespace AegisIdentity.SharedKernel.Constants;

public static class TokenLifetimes
{
    public const int EmailConfirmationHours = 24;
    public const int PasswordResetMinutes   = 30;

    public const int AntiTimingAttackDelayMilliseconds = 50;
}
