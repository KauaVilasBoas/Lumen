namespace AegisIdentity.SharedKernel.Constants;

/// <summary>
/// Subject lines for outbound transactional emails.
/// Centralised so that command handlers and tests share the exact same string.
/// </summary>
public static class EmailSubjects
{
    /// <summary>Subject for the email-address confirmation message sent after registration.</summary>
    public const string EmailConfirmation = "Confirme seu email — AegisIdentity";

    /// <summary>Subject for the password-reset link email.</summary>
    public const string PasswordReset = "Redefina sua senha — AegisIdentity";
}
