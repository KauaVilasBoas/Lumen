namespace Lumen.SharedKernel.Constants;

public static class TokenLifetimes
{
    public static readonly TimeSpan EmailConfirmation = TimeSpan.FromHours(24);
    public static readonly TimeSpan PasswordReset = TimeSpan.FromMinutes(30);
}
